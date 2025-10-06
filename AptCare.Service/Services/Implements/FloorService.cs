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
    public class FloorService : BaseService<FloorService>, IFloorService
    {
        public FloorService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<FloorService> logger, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
        }
    }
}
