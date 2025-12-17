using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.CommonAreaObjectTypeDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AptCare.Service.Services.Implements
{
    public class CommonAreaObjectTypeService : BaseService<CommonAreaObjectTypeService>, ICommonAreaObjectTypeService
    {
        private readonly IRedisCacheService _cacheService;

        public CommonAreaObjectTypeService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<CommonAreaObjectTypeService> logger,
            IMapper mapper,
            IRedisCacheService cacheService) : base(unitOfWork, logger, mapper)
        {
            _cacheService = cacheService;
        }

        public async Task<string> CreateCommonAreaObjectTypeAsync(CommonAreaObjectTypeCreateDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var isDuplicate = await _unitOfWork.GetRepository<CommonAreaObjectType>().AnyAsync(
                    predicate: x => x.TypeName.ToLower() == dto.TypeName.ToLower()
                );

                if (isDuplicate)
                    throw new AppValidationException("Tên loại đối tượng đã tồn tại.", StatusCodes.Status409Conflict);

                var commonAreaObjectType = _mapper.Map<CommonAreaObjectType>(dto);
                commonAreaObjectType.Status = ActiveStatus.Active;

                await _unitOfWork.GetRepository<CommonAreaObjectType>().InsertAsync(commonAreaObjectType);
                await _unitOfWork.CommitAsync();

                if (dto.MaintenanceTasks != null && dto.MaintenanceTasks.Any())
                {
                    var duplicateDisplayOrders = dto.MaintenanceTasks
                        .GroupBy(t => t.DisplayOrder)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();

                    if (duplicateDisplayOrders.Any())
                        throw new AppValidationException(
                            $"Thứ tự hiển thị bị trùng lặp: {string.Join(", ", duplicateDisplayOrders)}",
                            StatusCodes.Status400BadRequest);

                    var duplicateTaskNames = dto.MaintenanceTasks
                        .GroupBy(t => t.TaskName.ToLower())
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();

                    if (duplicateTaskNames.Any())
                        throw new AppValidationException(
                            $"Tên nhiệm vụ bị trùng lặp: {string.Join(", ", duplicateTaskNames)}",
                            StatusCodes.Status400BadRequest);

                    foreach (var taskDto in dto.MaintenanceTasks)
                    {
                        var maintenanceTask = new MaintenanceTask
                        {
                            CommonAreaObjectTypeId = commonAreaObjectType.CommonAreaObjectTypeId,
                            TaskName = taskDto.TaskName,
                            TaskDescription = taskDto.TaskDescription,
                            RequiredTools = taskDto.RequiredTools,
                            DisplayOrder = taskDto.DisplayOrder,
                            EstimatedDurationMinutes = taskDto.EstimatedDurationMinutes,
                            Status = ActiveStatus.Active
                        };

                        await _unitOfWork.GetRepository<MaintenanceTask>().InsertAsync(maintenanceTask);
                    }

                    await _unitOfWork.CommitAsync();
                }

                await _cacheService.RemoveByPrefixAsync("common_area_object_type");
                await _unitOfWork.CommitTransactionAsync();

                return "Tạo loại đối tượng khu vực chung mới thành công";
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<string> UpdateCommonAreaObjectTypeAsync(int id, CommonAreaObjectTypeUpdateDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var commonAreaObjectType = await _unitOfWork.GetRepository<CommonAreaObjectType>()
                    .SingleOrDefaultAsync(predicate: x => x.CommonAreaObjectTypeId == id);

                if (commonAreaObjectType is null)
                    throw new AppValidationException("Loại đối tượng khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

                var isDuplicate = await _unitOfWork.GetRepository<CommonAreaObjectType>().AnyAsync(
                    predicate: x => x.CommonAreaObjectTypeId != id &&
                                  x.TypeName.ToLower() == dto.TypeName.ToLower()
                );

                if (isDuplicate)
                    throw new AppValidationException("Tên loại đối tượng đã tồn tại.", StatusCodes.Status409Conflict);

                _mapper.Map(dto, commonAreaObjectType);
                _unitOfWork.GetRepository<CommonAreaObjectType>().UpdateAsync(commonAreaObjectType);

                if (dto.MaintenanceTasks != null)
                {
                    // Kiểm tra trùng lặp DisplayOrder trong danh sách mới
                    var duplicateDisplayOrders = dto.MaintenanceTasks
                        .GroupBy(t => t.DisplayOrder)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();

                    if (duplicateDisplayOrders.Any())
                        throw new AppValidationException(
                            $"Thứ tự hiển thị bị trùng lặp: {string.Join(", ", duplicateDisplayOrders)}",
                            StatusCodes.Status400BadRequest);

                    // Kiểm tra trùng lặp TaskName trong danh sách mới
                    var duplicateTaskNames = dto.MaintenanceTasks
                        .GroupBy(t => t.TaskName.ToLower())
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();

                    if (duplicateTaskNames.Any())
                        throw new AppValidationException(
                            $"Tên nhiệm vụ bị trùng lặp: {string.Join(", ", duplicateTaskNames)}",
                            StatusCodes.Status400BadRequest);

                    // Lấy các task hiện tại
                    var existingTasks = await _unitOfWork.GetRepository<MaintenanceTask>().GetListAsync(
                        predicate: t => t.CommonAreaObjectTypeId == id,
                        include: i => i.Include(x => x.RepairRequestTasks)
                    );

                    // Xóa các task không còn trong danh sách mới
                    var tasksToDelete = existingTasks
                        .Where(et => !dto.MaintenanceTasks.Any(nt =>
                            nt.TaskName.ToLower() == et.TaskName.ToLower()))
                        .ToList();

                    foreach (var taskToDelete in tasksToDelete)
                    {
                        if (taskToDelete.RepairRequestTasks?.Any() == true)
                            throw new AppValidationException(
                                $"Không thể xóa nhiệm vụ '{taskToDelete.TaskName}' vì đang có yêu cầu sửa chữa liên kết.",
                                StatusCodes.Status400BadRequest);

                        _unitOfWork.GetRepository<MaintenanceTask>().DeleteAsync(taskToDelete);
                    }

                    // Cập nhật hoặc thêm mới các task
                    foreach (var taskDto in dto.MaintenanceTasks)
                    {
                        var existingTask = existingTasks.FirstOrDefault(
                            et => et.TaskName.ToLower() == taskDto.TaskName.ToLower());

                        if (existingTask != null)
                        {
                            // Cập nhật task hiện có
                            existingTask.TaskDescription = taskDto.TaskDescription;
                            existingTask.RequiredTools = taskDto.RequiredTools;
                            existingTask.DisplayOrder = taskDto.DisplayOrder;
                            existingTask.EstimatedDurationMinutes = taskDto.EstimatedDurationMinutes;

                            _unitOfWork.GetRepository<MaintenanceTask>().UpdateAsync(existingTask);
                        }
                        else
                        {
                            // Thêm task mới
                            var newTask = new MaintenanceTask
                            {
                                CommonAreaObjectTypeId = id,
                                TaskName = taskDto.TaskName,
                                TaskDescription = taskDto.TaskDescription,
                                RequiredTools = taskDto.RequiredTools,
                                DisplayOrder = taskDto.DisplayOrder,
                                EstimatedDurationMinutes = taskDto.EstimatedDurationMinutes,
                                Status = ActiveStatus.Active
                            };

                            await _unitOfWork.GetRepository<MaintenanceTask>().InsertAsync(newTask);
                        }
                    }

                    await UpdateEstimatedDurationMaintenanceScheduleAsync(id);
                }

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();
                await _cacheService.RemoveAsync($"common_area_object_type:{id}");
                await _cacheService.RemoveByPrefixAsync("common_area_object_type:list");
                await _cacheService.RemoveByPrefixAsync("common_area_object_type:paginate");

                return "Cập nhật loại đối tượng khu vực chung thành công";
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<string> DeleteCommonAreaObjectTypeAsync(int id)
        {
            try
            {
                var commonAreaObjectType = await _unitOfWork.GetRepository<CommonAreaObjectType>()
                    .SingleOrDefaultAsync(
                        predicate: x => x.CommonAreaObjectTypeId == id,
                        include: i => i.Include(x => x.CommonAreaObjects)
                                       .Include(x => x.MaintenanceTasks)
                    );

                if (commonAreaObjectType is null)
                    throw new AppValidationException("Loại đối tượng khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

                if (commonAreaObjectType.CommonAreaObjects?.Any() == true)
                    throw new AppValidationException("Không thể xóa loại đối tượng đang có đối tượng liên kết.", StatusCodes.Status400BadRequest);

                if (commonAreaObjectType.MaintenanceTasks?.Any() == true)
                    throw new AppValidationException("Không thể xóa loại đối tượng đang có nhiệm vụ bảo trì liên kết.", StatusCodes.Status400BadRequest);

                _unitOfWork.GetRepository<CommonAreaObjectType>().DeleteAsync(commonAreaObjectType);
                await _unitOfWork.CommitAsync();

                await _cacheService.RemoveByPrefixAsync("common_area_object_type");

                return "Xóa loại đối tượng khu vực chung thành công";
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<string> ActivateCommonAreaObjectTypeAsync(int id)
        {
            try
            {
                var commonAreaObjectType = await _unitOfWork.GetRepository<CommonAreaObjectType>()
                    .SingleOrDefaultAsync(predicate: x => x.CommonAreaObjectTypeId == id);

                if (commonAreaObjectType is null)
                    throw new AppValidationException("Loại đối tượng khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

                if (commonAreaObjectType.Status == ActiveStatus.Active)
                    throw new AppValidationException("Loại đối tượng đã ở trạng thái hoạt động.");

                commonAreaObjectType.Status = ActiveStatus.Active;
                _unitOfWork.GetRepository<CommonAreaObjectType>().UpdateAsync(commonAreaObjectType);
                await _unitOfWork.CommitAsync();

                await _cacheService.RemoveAsync($"common_area_object_type:{id}");
                await _cacheService.RemoveByPrefixAsync("common_area_object_type:list");
                await _cacheService.RemoveByPrefixAsync("common_area_object_type:paginate");

                return "Kích hoạt loại đối tượng khu vực chung thành công";
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<string> DeactivateCommonAreaObjectTypeAsync(int id)
        {
            try
            {
                var commonAreaObjectType = await _unitOfWork.GetRepository<CommonAreaObjectType>()
                    .SingleOrDefaultAsync(predicate: x => x.CommonAreaObjectTypeId == id);

                if (commonAreaObjectType is null)
                    throw new AppValidationException("Loại đối tượng khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

                if (commonAreaObjectType.Status == ActiveStatus.Inactive)
                    throw new AppValidationException("Loại đối tượng đã ở trạng thái ngưng hoạt động.");

                commonAreaObjectType.Status = ActiveStatus.Inactive;
                _unitOfWork.GetRepository<CommonAreaObjectType>().UpdateAsync(commonAreaObjectType);
                await _unitOfWork.CommitAsync();

                await _cacheService.RemoveAsync($"common_area_object_type:{id}");
                await _cacheService.RemoveByPrefixAsync("common_area_object_type:list");
                await _cacheService.RemoveByPrefixAsync("common_area_object_type:paginate");

                return "Vô hiệu hóa loại đối tượng khu vực chung thành công";
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<CommonAreaObjectTypeDto> GetCommonAreaObjectTypeByIdAsync(int id)
        {
            var cacheKey = $"common_area_object_type:{id}";

            var cachedResult = await _cacheService.GetAsync<CommonAreaObjectTypeDto>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var commonAreaObjectType = await _unitOfWork.GetRepository<CommonAreaObjectType>().SingleOrDefaultAsync(
                selector: x => _mapper.Map<CommonAreaObjectTypeDto>(x),
                predicate: p => p.CommonAreaObjectTypeId == id,
                include: i => i.Include(x => x.MaintenanceTasks)
            );

            if (commonAreaObjectType == null)
                throw new AppValidationException("Loại đối tượng khu vực chung không tồn tại.", StatusCodes.Status404NotFound);

            await _cacheService.SetAsync(cacheKey, commonAreaObjectType, TimeSpan.FromMinutes(30));

            return commonAreaObjectType;
        }

        public async Task<IPaginate<CommonAreaObjectTypeDto>> GetPaginateCommonAreaObjectTypeAsync(PaginateDto dto)
        {
            int page = dto.page > 0 ? dto.page : 1;
            int size = dto.size > 0 ? dto.size : 10;
            string search = dto.search?.ToLower() ?? string.Empty;
            string filter = dto.filter?.ToLower() ?? string.Empty;
            string sortBy = dto.sortBy?.ToLower() ?? string.Empty;

            var cacheKey = $"common_area_object_type:paginate:page:{page}:size:{size}:search:{search}:filter:{filter}:sort:{sortBy}";

            var cachedResult = await _cacheService.GetAsync<Paginate<CommonAreaObjectTypeDto>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            ActiveStatus? filterStatus = null;
            if (!string.IsNullOrEmpty(filter))
            {
                if (Enum.TryParse<ActiveStatus>(filter, true, out var parsedStatus))
                {
                    filterStatus = parsedStatus;
                }
            }

            Expression<Func<CommonAreaObjectType, bool>> predicate = p =>
                (string.IsNullOrEmpty(search) ||
                    p.TypeName.ToLower().Contains(search) ||
                    (p.Description != null && p.Description.ToLower().Contains(search))) &&
                (string.IsNullOrEmpty(filter) || filterStatus == p.Status);

            var result = await _unitOfWork.GetRepository<CommonAreaObjectType>().GetPagingListAsync(
                selector: s => _mapper.Map<CommonAreaObjectTypeDto>(s),
                predicate: predicate,
                include: i => i.Include(x => x.MaintenanceTasks),
                orderBy: BuildOrderBy(sortBy),
                page: page,
                size: size
            );

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));

            return result;
        }

        public async Task<IEnumerable<CommonAreaObjectTypeDto>> GetCommonAreaObjectTypesAsync()
        {
            var cacheKey = "common_area_object_type:list:active";

            var cachedResult = await _cacheService.GetAsync<IEnumerable<CommonAreaObjectTypeDto>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var result = await _unitOfWork.GetRepository<CommonAreaObjectType>().GetListAsync(
                selector: s => _mapper.Map<CommonAreaObjectTypeDto>(s),
                predicate: p => p.Status == ActiveStatus.Active,
                include: i => i.Include(x => x.MaintenanceTasks)
            );

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(1));

            return result;
        }

        private Func<IQueryable<CommonAreaObjectType>, IOrderedQueryable<CommonAreaObjectType>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy))
                return q => q.OrderByDescending(p => p.CommonAreaObjectTypeId);

            return sortBy.ToLower() switch
            {
                "name" => q => q.OrderBy(p => p.TypeName),
                "name_desc" => q => q.OrderByDescending(p => p.TypeName),
                "status" => q => q.OrderBy(p => p.Status),
                "status_desc" => q => q.OrderByDescending(p => p.Status),
                _ => q => q.OrderByDescending(p => p.CommonAreaObjectTypeId)
            };
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
