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
    public class ApartmentService : BaseService<ApartmentService>, IApartmentService
    {
        public ApartmentService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<ApartmentService> logger, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
        }
    }
}
