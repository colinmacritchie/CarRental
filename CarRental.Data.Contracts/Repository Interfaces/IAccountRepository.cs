﻿using System;
using System.Collections.Generic;
using System.Linq;
using CarRental.Business.Entities;
using Core.Common.Contracts;

namespace CarRental.Data.Contracts
{
    public interface IAccountRepository : IDataRepository<Account>
    {
        Account GetByLogin(string login);
    }
}
