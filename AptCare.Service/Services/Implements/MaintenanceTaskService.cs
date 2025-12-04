using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.MaintenanceTaskDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AptCare.Service.Services.Implements
{
    public class MaintenanceTaskService : BaseService<MaintenanceTaskService>, IMaintenanceTaskService
    {
        private readonly IRedisCacheService _cacheService;

        public MaintenanceTaskService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<MaintenanceTaskService> logger,
            IMapper mapper,
            IRedisCacheService cacheService) : base(unitOfWork, logger, mapper)
        {
            _cacheService = cacheService;
        }

        public async Task<string> CreateMaintenanceTaskAsync(MaintenanceTaskCreateDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync(); 

                var objectType = await _unitOfWork.GetRepository<CommonAreaObjectType>()
                    .SingleOrDefaultAsync(predicate: x => x.CommonAreaObjectTypeId == dto.CommonAreaObjectTypeId);

                if (objectType is null)
                    throw new AppValidationException("Loại đối tượng khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

                if (objectType.Status == ActiveStatus.Inactive)
                    throw new AppValidationException("Loại đối tượng khu vực chung đã ngưng hoạt động.");

                var isDuplicate = await _unitOfWork.GetRepository<MaintenanceTask>().AnyAsync(
                    predicate: x => x.CommonAreaObjectTypeId == dto.CommonAreaObjectTypeId &&
                                  x.TaskName.ToLower() == dto.TaskName.ToLower()
                );

                if (isDuplicate)
                    throw new AppValidationException("Tên nhiệm vụ đã tồn tại cho loại đối tượng này.", StatusCodes.Status409Conflict);

                var isDuplicateDisplayOrder = await _unitOfWork.GetRepository<MaintenanceTask>().AnyAsync(
                    predicate: x => x.CommonAreaObjectTypeId == dto.CommonAreaObjectTypeId &&
                                  x.DisplayOrder == dto.DisplayOrder
                );

                if (isDuplicateDisplayOrder)
                    throw new AppValidationException("Thứ tự hiển thị đã tồn tại cho loại đối tượng này.", StatusCodes.Status409Conflict);

                var maintenanceTask = _mapper.Map<MaintenanceTask>(dto);
                maintenanceTask.Status = ActiveStatus.Active;

                await _unitOfWork.GetRepository<MaintenanceTask>().InsertAsync(maintenanceTask);
                await _unitOfWork.CommitAsync();

                await UpdateEstimatedDurationMaintenanceScheduleAsync(dto.CommonAreaObjectTypeId);

                await _cacheService.RemoveByPrefixAsync("maintenance_task");

                await _unitOfWork.CommitTransactionAsync();

                return "Tạo nhiệm vụ bảo trì mới thành công";
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<string> UpdateMaintenanceTaskAsync(int id, MaintenanceTaskUpdateDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var maintenanceTask = await _unitOfWork.GetRepository<MaintenanceTask>()
                    .SingleOrDefaultAsync(predicate: x => x.MaintenanceTaskId == id);

                if (maintenanceTask is null)
                    throw new AppValidationException("Nhiệm vụ bảo trì không tồn tại.", StatusCodes.Status404NotFound);

                var objectType = await _unitOfWork.GetRepository<CommonAreaObjectType>()
                    .SingleOrDefaultAsync(predicate: x => x.CommonAreaObjectTypeId == dto.CommonAreaObjectTypeId);

                if (objectType is null)
                    throw new AppValidationException("Loại đối tượng khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

                if (objectType.Status == ActiveStatus.Inactive)
                    throw new AppValidationException("Loại đối tượng khu vực chung đã ngưng hoạt động.");

                var isDuplicate = await _unitOfWork.GetRepository<MaintenanceTask>().AnyAsync(
                    predicate: x => x.MaintenanceTaskId != id &&
                                  x.CommonAreaObjectTypeId == dto.CommonAreaObjectTypeId &&
                                  x.TaskName.ToLower() == dto.TaskName.ToLower()
                );

                if (isDuplicate)
                    throw new AppValidationException("Tên nhiệm vụ đã tồn tại cho loại đối tượng này.", StatusCodes.Status409Conflict);

                var isDuplicateDisplayOrder = await _unitOfWork.GetRepository<MaintenanceTask>().AnyAsync(
                    predicate: x => x.MaintenanceTaskId != id &&
                                  x.CommonAreaObjectTypeId == dto.CommonAreaObjectTypeId &&
                                  x.DisplayOrder == dto.DisplayOrder
                );

                if (isDuplicateDisplayOrder)
                    throw new AppValidationException("Thứ tự hiển thị đã tồn tại cho loại đối tượng này.", StatusCodes.Status409Conflict);

                _mapper.Map(dto, maintenanceTask);
                _unitOfWork.GetRepository<MaintenanceTask>().UpdateAsync(maintenanceTask);
                await _unitOfWork.CommitAsync();

                await UpdateEstimatedDurationMaintenanceScheduleAsync(dto.CommonAreaObjectTypeId);

                await _cacheService.RemoveAsync($"maintenance_task:{id}");
                await _cacheService.RemoveByPrefixAsync("maintenance_task:list");
                await _cacheService.RemoveByPrefixAsync("maintenance_task:paginate");

                await _unitOfWork.CommitTransactionAsync();

                return "Cập nhật nhiệm vụ bảo trì thành công";
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<string> DeleteMaintenanceTaskAsync(int id)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var maintenanceTask = await _unitOfWork.GetRepository<MaintenanceTask>()
                    .SingleOrDefaultAsync(
                        predicate: x => x.MaintenanceTaskId == id,
                        include: i => i.Include(x => x.RepairRequestTasks)
                    );

                if (maintenanceTask is null)
                    throw new AppValidationException("Nhiệm vụ bảo trì không tồn tại.", StatusCodes.Status404NotFound);

                if (maintenanceTask.RepairRequestTasks?.Any() == true)
                    throw new AppValidationException("Không thể xóa nhiệm vụ bảo trì đang có yêu cầu sửa chữa liên kết.", StatusCodes.Status400BadRequest);

                _unitOfWork.GetRepository<MaintenanceTask>().DeleteAsync(maintenanceTask);
                await _unitOfWork.CommitAsync();


                await UpdateEstimatedDurationMaintenanceScheduleAsync(maintenanceTask.CommonAreaObjectTypeId);

                await _unitOfWork.CommitTransactionAsync();

                await _cacheService.RemoveByPrefixAsync("maintenance_task");

                return "Xóa nhiệm vụ bảo trì thành công";
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<string> ActivateMaintenanceTaskAsync(int id)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var maintenanceTask = await _unitOfWork.GetRepository<MaintenanceTask>()
                    .SingleOrDefaultAsync(
                        predicate: x => x.MaintenanceTaskId == id,
                        include: i => i.Include(x => x.CommonAreaObjectType)
                    );

                if (maintenanceTask is null)
                    throw new AppValidationException("Nhiệm vụ bảo trì không tồn tại.", StatusCodes.Status404NotFound);

                if (maintenanceTask.Status == ActiveStatus.Active)
                    throw new AppValidationException("Nhiệm vụ bảo trì đã ở trạng thái hoạt động.");

                if (maintenanceTask.CommonAreaObjectType.Status == ActiveStatus.Inactive)
                    throw new AppValidationException("Không thể kích hoạt nhiệm vụ khi loại đối tượng đã ngưng hoạt động.");

                maintenanceTask.Status = ActiveStatus.Active;
                _unitOfWork.GetRepository<MaintenanceTask>().UpdateAsync(maintenanceTask);
                await _unitOfWork.CommitAsync();

                await UpdateEstimatedDurationMaintenanceScheduleAsync(maintenanceTask.CommonAreaObjectTypeId);

                await _cacheService.RemoveAsync($"maintenance_task:{id}");
                await _cacheService.RemoveByPrefixAsync("maintenance_task:list");
                await _cacheService.RemoveByPrefixAsync("maintenance_task:paginate");

                await _unitOfWork.CommitTransactionAsync();

                return "Kích hoạt nhiệm vụ bảo trì thành công";
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<string> DeactivateMaintenanceTaskAsync(int id)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var maintenanceTask = await _unitOfWork.GetRepository<MaintenanceTask>()
                    .SingleOrDefaultAsync(
                        predicate: x => x.MaintenanceTaskId == id,
                        include: i => i.Include(x => x.CommonAreaObjectType)
                    );

                if (maintenanceTask is null)
                    throw new AppValidationException("Nhiệm vụ bảo trì không tồn tại.", StatusCodes.Status404NotFound);

                if (maintenanceTask.Status == ActiveStatus.Inactive)
                    throw new AppValidationException("Nhiệm vụ bảo trì đã ở trạng thái ngưng hoạt động.");

                maintenanceTask.Status = ActiveStatus.Inactive;
                _unitOfWork.GetRepository<MaintenanceTask>().UpdateAsync(maintenanceTask);
                await _unitOfWork.CommitAsync();

                await UpdateEstimatedDurationMaintenanceScheduleAsync(maintenanceTask.CommonAreaObjectTypeId);

                await _cacheService.RemoveAsync($"maintenance_task:{id}");
                await _cacheService.RemoveByPrefixAsync("maintenance_task:list");
                await _cacheService.RemoveByPrefixAsync("maintenance_task:paginate");

                await _unitOfWork.CommitTransactionAsync();

                return "Vô hiệu hóa nhiệm vụ bảo trì thành công";
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<MaintenanceTaskDto> GetMaintenanceTaskByIdAsync(int id)
        {
            var cacheKey = $"maintenance_task:{id}";

            var cachedResult = await _cacheService.GetAsync<MaintenanceTaskDto>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var maintenanceTask = await _unitOfWork.GetRepository<MaintenanceTask>().SingleOrDefaultAsync(
                selector: x => _mapper.Map<MaintenanceTaskDto>(x),
                predicate: p => p.MaintenanceTaskId == id,
                include: i => i.Include(x => x.CommonAreaObjectType)
            );

            if (maintenanceTask == null)
                throw new AppValidationException("Nhiệm vụ bảo trì không tồn tại.", StatusCodes.Status404NotFound);

            await _cacheService.SetAsync(cacheKey, maintenanceTask, TimeSpan.FromMinutes(30));

            return maintenanceTask;
        }
        
        public async Task<IEnumerable<MaintenanceTaskBasicDto>> GetMaintenanceTasksByTypeAsync(int commonAreaObjectTypeId)
        {
            var cacheKey = $"maintenance_task:list:by_type:{commonAreaObjectTypeId}";

            var cachedResult = await _cacheService.GetAsync<IEnumerable<MaintenanceTaskBasicDto>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var isExistingType = await _unitOfWork.GetRepository<CommonAreaObjectType>().AnyAsync(
                predicate: x => x.CommonAreaObjectTypeId == commonAreaObjectTypeId);
            if (!isExistingType)
                throw new AppValidationException("Loại đối tượng khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

            var result = await _unitOfWork.GetRepository<MaintenanceTask>().GetListAsync(
                selector: s => _mapper.Map<MaintenanceTaskBasicDto>(s),
                predicate: p => p.CommonAreaObjectTypeId == commonAreaObjectTypeId &&
                              p.Status == ActiveStatus.Active,
                orderBy: o => o.OrderBy(x => x.DisplayOrder).ThenBy(x => x.TaskName)
            );

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(1));

            return result;
        }

        private async Task UpdateEstimatedDurationMaintenanceScheduleAsync(int commonAreaObjectTypeId)
        {
            var maintenanceTasks = await _unitOfWork.GetRepository<MaintenanceTask>().GetListAsync(
                predicate: mt => mt.CommonAreaObjectTypeId == commonAreaObjectTypeId &&
                                 mt.Status == ActiveStatus.Active
            );

            var totalEstimatedDurationHours = maintenanceTasks.Sum(mt => mt.EstimatedDurationMinutes) / 60;

            var commonAreaObjects = await _unitOfWork.GetRepository<CommonAreaObject>().GetListAsync(
                predicate: cao => cao.CommonAreaObjectTypeId == commonAreaObjectTypeId,
                include: i => i.Include(x => x.MaintenanceSchedule)
            );

            foreach (var commonAreaObject in commonAreaObjects)
            {
                if (commonAreaObject.MaintenanceSchedule != null)
                {
                    commonAreaObject.MaintenanceSchedule.EstimatedDuration = totalEstimatedDurationHours;
                    _unitOfWork.GetRepository<MaintenanceSchedule>().UpdateAsync(commonAreaObject.MaintenanceSchedule);
                }
            }

            await _unitOfWork.CommitAsync();
        }
    }
}