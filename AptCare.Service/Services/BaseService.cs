using AptCare.Repository;
using AptCare.Repository.UnitOfWork;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services
{
    public abstract class BaseService<T> where T : class
    {
        protected IUnitOfWork<AptCareSystemDBContext> _unitOfWork;
        protected ILogger<T> _logger;
        protected IMapper _mapper;

        public BaseService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<T> logger, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
        }


    }
}
