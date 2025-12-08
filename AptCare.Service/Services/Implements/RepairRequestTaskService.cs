using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.InspectionReporDtos;
using AptCare.Service.Dtos.RepairRequestTaskDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<string> UpdateRepairRequestTasksStatusAsync(int repairRequestId, List<RequestTaskStatusUpdateDto> updatedTasks)
        {
            try
            {
                var repairRequestRepo = _unitOfWork.GetRepository<RepairRequest>();
                var repairRequest = await repairRequestRepo.SingleOrDefaultAsync(
                    predicate: e => e.RepairRequestId == repairRequestId,
                    include: e => e.Include(r => r.RepairRequestTasks)
                );

                if (repairRequest == null)
                    throw new AppValidationException("Không tìm thấy yêu cầu sửa chữa.");

                if (updatedTasks == null || !updatedTasks.Any())
                    throw new AppValidationException("Chưa có công việc nào được cập nhật.", StatusCodes.Status400BadRequest);

                var allRepairRequestTasks = repairRequest.RepairRequestTasks?.ToList() ?? new List<RepairRequestTask>();

                if (!allRepairRequestTasks.Any())
                    throw new AppValidationException("Yêu cầu sửa chữa không có nhiệm vụ nào.", StatusCodes.Status400BadRequest);

                var updatedTaskIds = updatedTasks.Select(t => t.RepairRequestTaskId).ToHashSet();
                var allTaskIds = allRepairRequestTasks.Select(t => t.RepairRequestTaskId).ToHashSet();

                var missingTaskIds = allTaskIds.Except(updatedTaskIds).ToList();
                if (missingTaskIds.Any())
                {
                    var missingTaskNames = allRepairRequestTasks
                        .Where(t => missingTaskIds.Contains(t.RepairRequestTaskId))
                        .Select(t => t.TaskName)
                        .ToList();

                    throw new AppValidationException(
                        $"Chưa cập nhật đủ tất cả nhiệm vụ. Còn thiếu: {string.Join(", ", missingTaskNames)}",
                        StatusCodes.Status400BadRequest);
                }

                var invalidTaskIds = updatedTaskIds.Except(allTaskIds).ToList();
                if (invalidTaskIds.Any())
                {
                    throw new AppValidationException(
                        $"Có nhiệm vụ không thuộc yêu cầu sửa chữa này. Task IDs: {string.Join(", ", invalidTaskIds)}",
                        StatusCodes.Status400BadRequest);
                }

                var incompleteTasks = updatedTasks
                    .Where(t => t.Status == TaskCompletionStatus.Pending)
                    .ToList();

                if (incompleteTasks.Any())
                {
                    var incompleteTaskNames = allRepairRequestTasks
                        .Where(t => incompleteTasks.Select(it => it.RepairRequestTaskId).Contains(t.RepairRequestTaskId))
                        .Select(t => t.TaskName)
                        .ToList();

                    throw new AppValidationException(
                        $"Tất cả nhiệm vụ phải được hoàn thành trước khi cập nhật. Nhiệm vụ chưa hoàn thành: {string.Join(", ", incompleteTaskNames)}",
                        StatusCodes.Status400BadRequest);
                }

                await _unitOfWork.BeginTransactionAsync();

                var repairRequestTaskRepo = _unitOfWork.GetRepository<RepairRequestTask>();
                foreach (var updatedTask in updatedTasks)
                {
                    var task = allRepairRequestTasks.FirstOrDefault(t => t.RepairRequestTaskId == updatedTask.RepairRequestTaskId);
                    if (task != null)
                    {
                        task.Status = updatedTask.Status;
                        task.TechnicianNote = updatedTask.TechnicianNote;
                        task.InspectionResult = updatedTask.InspectionResult;
                        task.CompletedAt = DateTime.Now;
                        task.CompletedByUserId = _userContext.CurrentUserId;

                        repairRequestTaskRepo.UpdateAsync(task);
                    }
                }

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                foreach (var updatedtaskTask in updatedTasks)
                {
                    await _cacheService.RemoveAsync($"repair_request_task:{updatedtaskTask.RepairRequestTaskId}");
                }
                await _cacheService.RemoveByPrefixAsync($"repair_request_task:list:repair_request:{repairRequestId}");


                _logger.LogInformation("Updated {TaskCount} tasks for RepairRequest {RepairRequestId}",
                    updatedTasks.Count, repairRequestId);

                return "Cập nhật trạng thái nhiệm vụ thành công.";
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error updating tasks for RepairRequestId: {RepairRequestId}", repairRequestId);
                throw;
            }
        }
    }
}