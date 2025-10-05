using AptCare.Repository;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Implements
{
    public class AccountService : BaseService<AccountService>, IAccountService
    {
        public AccountService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<AccountService> logger, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
        }
    }
}
