using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.RepairRequestTaskDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AptCare.Service.Services.Implements
{
    public class RepairRequestTaskService : BaseService<RepairRequestTaskService>, IRepairRequestTaskService
    {
        private readonly IRedisCacheService _cacheService;
        private readonly IUserContext _userContext;

        public RepairRequestTaskService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<RepairRequestTaskService> logger,
            IMapper mapper,
            IRedisCacheService cacheService,
            IUserContext userContext) : base(unitOfWork, logger, mapper)
        {
            _cacheService = cacheService;
            _userContext = userContext;
        }
       
        public async Task<string> UpdateRepairRequestTaskStatusAsync(int id, RepairRequestTaskStatusUpdateDto dto)
        {
            var repairRequestTask = await _unitOfWork.GetRepository<RepairRequestTask>()
                .SingleOrDefaultAsync(predicate: x => x.RepairRequestTaskId == id);

            if (repairRequestTask is null)
                throw new AppValidationException("Nhiệm vụ sửa chữa không tồn tại.", StatusCodes.Status404NotFound);

            _mapper.Map(dto, repairRequestTask);
            repairRequestTask.CompletedByUserId = _userContext.CurrentUserId;

            _unitOfWork.GetRepository<RepairRequestTask>().UpdateAsync(repairRequestTask);
            await _unitOfWork.CommitAsync();

            await _cacheService.RemoveAsync($"repair_request_task:{id}");
            await _cacheService.RemoveByPrefixAsync($"repair_request_task:list:repair_request:{repairRequestTask.RepairRequestId}");

            return "Cập nhật trạng thái nhiệm vụ sửa chữa thành công";
        }

        public async Task<RepairRequestTaskDto> GetRepairRequestTaskByIdAsync(int id)
        {
            var cacheKey = $"repair_request_task:{id}";

            var cachedResult = await _cacheService.GetAsync<RepairRequestTaskDto>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var repairRequestTask = await _unitOfWork.GetRepository<RepairRequestTask>().SingleOrDefaultAsync(
                selector: x => _mapper.Map<RepairRequestTaskDto>(x),
                predicate: p => p.RepairRequestTaskId == id,
                include: i => i.Include(x => x.CompletedBy)
            );

            if (repairRequestTask == null)
                throw new AppValidationException("Nhiệm vụ sửa chữa không tồn tại.", StatusCodes.Status404NotFound);

            await _cacheService.SetAsync(cacheKey, repairRequestTask, TimeSpan.FromMinutes(30));

            return repairRequestTask;
        }

        public async Task<IEnumerable<RepairRequestTaskDto>> GetRepairRequestTasksByRepairRequestIdAsync(int repairRequestId)
        {
            var cacheKey = $"repair_request_task:list:repair_request:{repairRequestId}";

            var cachedResult = await _cacheService.GetAsync<IEnumerable<RepairRequestTaskDto>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var isExistingRepairRequest = await _unitOfWork.GetRepository<RepairRequest>().AnyAsync(
                predicate: x => x.RepairRequestId == repairRequestId);
            if (!isExistingRepairRequest)
                throw new AppValidationException("Yêu cầu sửa chữa không tồn tại.", StatusCodes.Status404NotFound);

            var result = await _unitOfWork.GetRepository<RepairRequestTask>().GetListAsync(
                selector: s => _mapper.Map<RepairRequestTaskDto>(s),
                predicate: p => p.RepairRequestId == repairRequestId,
                include: i => i.Include(x => x.CompletedBy),
                orderBy: o => o.OrderBy(x => x.DisplayOrder)
            );

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30));

            return result;
        }
    }
}