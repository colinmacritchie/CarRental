using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using CarRental.Client.Contracts;
using CarRental.Client.Entities;
using Core.Common;
using Core.Common.Contracts;
using Core.Common.UI.Core;

namespace CarRental.Admin.ViewModels
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class MaintainCarsViewModel : ViewModelBase
    {
        [ImportingConstructor]
        public MaintainCarsViewModel(IServiceFactory serviceFactory)
        {
            _ServiceFactory = serviceFactory;

            EditCarCommand = new DelegateCommand<Car>(OnEditCarCommand);
            AddCarCommand = new DelegateCommand<object>(OnAddCarCommand);
            DeleteCarCommand = new DelegateCommand<Car>(OnDeleteCarCommand);
        }

        IServiceFactory _ServiceFactory;

        public override string ViewTitle
        {
            get { return "Maintain Cars"; }
        }

        ObservableCollection<Car> _Cars;
        EditCarViewModel _CurrentCarViewModel;

        public DelegateCommand<Car> EditCarCommand { get; private set; }
        public DelegateCommand<object> AddCarCommand { get; private set; }
        public DelegateCommand<Car> DeleteCarCommand { get; private set; }

        public event CancelEventHandler ConfirmDelete;
        public event EventHandler<ErrorMessageEventArgs> ErrorOccured;
        
        public ObservableCollection<Car> Cars
        {
            get { return _Cars; }
            set
            {
                if (_Cars != value)
                {
                    _Cars = value;
                    OnPropertyChanged(() => Cars, false);
                }
            }
        }

        public EditCarViewModel CurrentCarViewModel
        {
            get { return _CurrentCarViewModel; }
            set
            {
                if (_CurrentCarViewModel != value)
                {
                    _CurrentCarViewModel = value;
                    OnPropertyChanged(() => CurrentCarViewModel, false);
                }
            }
        }

        protected override void OnViewLoaded()
        {
            _Cars = new ObservableCollection<Car>();

            WithClient<IInventoryService>(_ServiceFactory.CreateClient<IInventoryService>(), inventoryClient =>
            {
                Car[] cars = inventoryClient.GetAllCars();
                if (cars != null)
                {
                    foreach (Car car in cars)
                        _Cars.Add(car);
                }
            });
        }

        void OnAddCarCommand(object arg)
        {
            Car car = new Car();
            CurrentCarViewModel = new EditCarViewModel(_ServiceFactory, car);
            CurrentCarViewModel.CarUpdated += CurrentCarViewModel_CarUpdated;
            CurrentCarViewModel.CancelEditCar += CurrentCarViewModel_CancelEditCar;
        }

        void OnEditCarCommand(Car car)
        {
            if (car != null)
            {
                CurrentCarViewModel = new EditCarViewModel(_ServiceFactory, car);
                CurrentCarViewModel.CarUpdated += CurrentCarViewModel_CarUpdated;
                CurrentCarViewModel.CancelEditCar += CurrentCarViewModel_CancelEditCar;
            }
        }

        void CurrentCarViewModel_CarUpdated(object sender, Support.CarEventArgs e)
        {
            if (!e.IsNew)
            {
                Car car = _Cars.Where(item => item.CarId == e.Car.CarId).FirstOrDefault();
                if (car != null)
                {
                    car.Description = e.Car.Description;
                    car.Color = e.Car.Color;
                    car.Year = e.Car.Year;
                    car.RentalPrice = e.Car.RentalPrice;
                }
            }
            else
                _Cars.Add(e.Car);

            CurrentCarViewModel = null;
        }

        void CurrentCarViewModel_CancelEditCar(object sender, EventArgs e)
        {
            CurrentCarViewModel = null;
        }

        void OnDeleteCarCommand(Car car)
        {
            bool carIsRented = false;

            // check to see if car is currently rented
            WithClient<IRentalService>(_ServiceFactory.CreateClient<IRentalService>(), rentalClient =>
            {
                carIsRented = rentalClient.IsCarCurrentlyRented(car.CarId);
            });

            if (!carIsRented)
            {
                CancelEventArgs args = new CancelEventArgs();
                if (ConfirmDelete != null)
                    ConfirmDelete(this, args);

                if (!args.Cancel)
                {
                    try
                    {
                        WithClient<IInventoryService>(_ServiceFactory.CreateClient<IInventoryService>(), inventoryClient =>
                        {
                            inventoryClient.DeleteCar(car.CarId);
                            _Cars.Remove(car);
                        });
                    }
                    catch (FaultException ex)
                    {
                        if (ErrorOccured != null)
                            ErrorOccured(this, new ErrorMessageEventArgs(ex.Message));
                    }
                    catch (Exception ex)
                    {
                        if (ErrorOccured != null)
                            ErrorOccured(this, new ErrorMessageEventArgs(ex.Message));
                    }
                }
            }
            else
            {
                if (ErrorOccured != null)
                    ErrorOccured(this, new ErrorMessageEventArgs("Cannot delete this car. It is currently rented."));
            }
        }
    }
}