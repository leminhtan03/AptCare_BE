using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.ReportDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AptCare.Service.Services.Implements
{
    public class ReportService : BaseService<ReportService>, IReportService
    {
        private readonly IUserContext _userContext;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IRedisCacheService _cacheService;

        public ReportService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<ReportService> logger,
            IMapper mapper,
            IUserContext userContext,
            ICloudinaryService cloudinaryService,
            IRedisCacheService cacheService) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
            _cloudinaryService = cloudinaryService;
            _cacheService = cacheService;
        }

        public async Task<ReportDto> CreateReportAsync(ReportCreateDto dto)
        {
            try
            {
                var reportRepo = _unitOfWork.GetRepository<Report>();
                var commonAreaObjectRepo = _unitOfWork.GetRepository<CommonAreaObject>();
                var mediaRepo = _unitOfWork.GetRepository<Media>();

                await _unitOfWork.BeginTransactionAsync();

                var commonAreaObject = await commonAreaObjectRepo.SingleOrDefaultAsync(
                    predicate: cao => cao.CommonAreaObjectId == dto.CommonAreaObjectId,
                    include: i => i.Include(cao => cao.CommonArea));

                if (commonAreaObject == null)
                {
                    throw new AppValidationException(
                        "Đối tượng khu vực chung không tồn tại.",
                        StatusCodes.Status404NotFound);
                }

                if (commonAreaObject.Status == ActiveStatus.Inactive)
                {
                    throw new AppValidationException(
                        "Đối tượng khu vực chung đã ngưng hoạt động.",
                        StatusCodes.Status400BadRequest);
                }

                if (commonAreaObject.CommonArea.Status == ActiveStatus.Inactive)
                {
                    throw new AppValidationException(
                        "Khu vực chung đã ngưng hoạt động.",
                        StatusCodes.Status400BadRequest);
                }

                var newReport = _mapper.Map<Report>(dto);
                newReport.UserId = _userContext.CurrentUserId;

                await reportRepo.InsertAsync(newReport);
                await _unitOfWork.CommitAsync();

                // Upload files if provided
                if (dto.Files != null && dto.Files.Any())
                {
                    foreach (var file in dto.Files)
                    {
                        if (file == null || file.Length == 0)
                            throw new AppValidationException("File không hợp lệ.");

                        var filePath = await _cloudinaryService.UploadImageAsync(file);
                        if (string.IsNullOrEmpty(filePath))
                        {
                            throw new AppValidationException(
                                "Có lỗi xảy ra khi upload file.",
                                StatusCodes.Status500InternalServerError);
                        }

                        await mediaRepo.InsertAsync(new Media
                        {
                            Entity = nameof(Report),
                            EntityId = newReport.ReportId,
                            FileName = file.FileName,
                            FilePath = filePath,
                            ContentType = file.ContentType,
                            CreatedAt = DateTime.Now,
                            Status = ActiveStatus.Active
                        });
                    }
                }

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                // Clear cache after create
                await _cacheService.RemoveByPrefixAsync("report");

                // Return created report with details
                return await GetReportByIdAsync(newReport.ReportId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi khi tạo Report");
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> UpdateReportAsync(int id, ReportUpdateDto dto)
        {
            var reportRepo = _unitOfWork.GetRepository<Report>();
            var commonAreaObjectRepo = _unitOfWork.GetRepository<CommonAreaObject>();

            var report = await reportRepo.SingleOrDefaultAsync(
                predicate: r => r.ReportId == id);

            if (report == null)
            {
                throw new AppValidationException(
                    "Báo cáo không tồn tại.",
                    StatusCodes.Status404NotFound);
            }

            // Check ownership
            if (report.UserId != _userContext.CurrentUserId)
            {
                throw new AppValidationException(
                    "Bạn không có quyền cập nhật báo cáo này.",
                    StatusCodes.Status403Forbidden);
            }

            // Validate CommonAreaObject
            var commonAreaObject = await commonAreaObjectRepo.SingleOrDefaultAsync(
                predicate: cao => cao.CommonAreaObjectId == dto.CommonAreaObjectId,
                include: i => i.Include(cao => cao.CommonArea));

            if (commonAreaObject == null)
            {
                throw new AppValidationException(
                    "Đối tượng khu vực chung không tồn tại.",
                    StatusCodes.Status404NotFound);
            }

            if (commonAreaObject.Status == ActiveStatus.Inactive)
            {
                throw new AppValidationException(
                    "Đối tượng khu vực chung đã ngưng hoạt động.",
                    StatusCodes.Status400BadRequest);
            }

            _mapper.Map(dto, report);
            reportRepo.UpdateAsync(report);
            await _unitOfWork.CommitAsync();

            // Clear cache after update
            await _cacheService.RemoveAsync($"report:{id}");
            await _cacheService.RemoveByPrefixAsync("report:list");
            await _cacheService.RemoveByPrefixAsync("report:paginate");

            return "Cập nhật báo cáo thành công.";
        }

        public async Task<string> DeleteReportAsync(int id)
        {
            var reportRepo = _unitOfWork.GetRepository<Report>();

            var report = await reportRepo.SingleOrDefaultAsync(
                predicate: r => r.ReportId == id);

            if (report == null)
            {
                throw new AppValidationException(
                    "Báo cáo không tồn tại.",
                    StatusCodes.Status404NotFound);
            }

            // Check ownership or admin rights
            if (report.UserId != _userContext.CurrentUserId)
            {
                throw new AppValidationException(
                    "Bạn không có quyền xóa báo cáo này.",
                    StatusCodes.Status403Forbidden);
            }

            reportRepo.DeleteAsync(report);
            await _unitOfWork.CommitAsync();

            // Clear cache after delete
            await _cacheService.RemoveByPrefixAsync("report");

            return "Xóa báo cáo thành công.";
        }

        public async Task<ReportDto> GetReportByIdAsync(int id)
        {
            var cacheKey = $"report:{id}";

            var cachedReport = await _cacheService.GetAsync<ReportDto>(cacheKey);
            if (cachedReport != null)
            {
                return cachedReport;
            }

            var reportRepo = _unitOfWork.GetRepository<Report>();

            var report = await reportRepo.SingleOrDefaultAsync(
                predicate: r => r.ReportId == id,
                include: i => i.Include(r => r.User)
                              .Include(r => r.CommonAreaObject)
                                  .ThenInclude(cao => cao.CommonArea)
                                    .ThenInclude(x => x.Floor));

            if (report == null)
            {
                throw new AppValidationException(
                    "Báo cáo không tồn tại.",
                    StatusCodes.Status404NotFound);
            }

            var result = _mapper.Map<ReportDto>(report);

            // Load media files
            var medias = await _unitOfWork.GetRepository<Media>().GetListAsync(
                selector: m => _mapper.Map<MediaDto>(m),
                predicate: m => m.Entity == nameof(Report) && m.EntityId == result.ReportId);

            result.Medias = medias.ToList();

            // Cache for 20 minutes
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(20));

            return result;
        }

        public async Task<IPaginate<ReportDto>> GetPaginateReportsAsync(ReportFilterDto filterDto)
        {
            int page = filterDto.page > 0 ? filterDto.page : 1;
            int size = filterDto.size > 0 ? filterDto.size : 10;
            string search = filterDto.search?.ToLower() ?? string.Empty;
            string filter = filterDto.filter?.ToLower() ?? string.Empty;
            string sortBy = filterDto.sortBy?.ToLower() ?? string.Empty;

            var cacheKey = $"report:paginate:page:{page}:size:{size}:search:{search}:filter:{filter}:sort:{sortBy}:from:{filterDto.Fromdate}:to:{filterDto.Todate}:obj:{filterDto.CommonAreaObjectId}:user:{filterDto.UserId}";

            var cachedResult = await _cacheService.GetAsync<Paginate<ReportDto>>(cacheKey);
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
            
            Expression<Func<Report, bool>> predicate = r =>
                (string.IsNullOrEmpty(search) ||
                 r.Title.ToLower().Contains(search) ||
                 (r.Description != null && r.Description.ToLower().Contains(search)) ||
                 r.CommonAreaObject.Name.ToLower().Contains(search) ||
                 r.CommonAreaObject.CommonArea.Name.ToLower().Contains(search)) &&
                (string.IsNullOrEmpty(filter) || r.Status == filterStatus) &&
                (!filterDto.Fromdate.HasValue ||
                 DateOnly.FromDateTime(r.CreatedAt) >= filterDto.Fromdate.Value) &&
                (!filterDto.Todate.HasValue ||
                 DateOnly.FromDateTime(r.CreatedAt) <= filterDto.Todate.Value) &&
                (!filterDto.CommonAreaObjectId.HasValue ||
                 r.CommonAreaObjectId == filterDto.CommonAreaObjectId.Value) &&
                (!filterDto.UserId.HasValue ||
                 r.UserId == filterDto.UserId.Value);

            var reportRepo = _unitOfWork.GetRepository<Report>();

            var paginateResult = await reportRepo.GetPagingListAsync(
                page: page,
                size: size,
                predicate: predicate,
                include: i => i.Include(r => r.User)
                              .Include(r => r.CommonAreaObject)
                                  .ThenInclude(cao => cao.CommonArea)
                                    .ThenInclude(x => x.Floor),
                orderBy: BuildOrderBy(filterDto.sortBy ?? string.Empty),
                selector: r => _mapper.Map<ReportDto>(r));

            foreach (var item in paginateResult.Items)
            {
                var medias = await _unitOfWork.GetRepository<Media>().GetListAsync(
                    selector: m => _mapper.Map<MediaDto>(m),
                    predicate: m => m.Entity == nameof(Report) && m.EntityId == item.ReportId && m.Status == ActiveStatus.Active);

                item.Medias = medias.ToList();
            }

            // Cache for 10 minutes
            await _cacheService.SetAsync(cacheKey, paginateResult, TimeSpan.FromMinutes(10));

            return paginateResult;
        }

        public async Task<IEnumerable<ReportBasicDto>> GetReportsByCommonAreaObjectAsync(int commonAreaObjectId)
        {
            var cacheKey = $"report:list:by_object:{commonAreaObjectId}";

            var cachedResult = await _cacheService.GetAsync<IEnumerable<ReportBasicDto>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var reportRepo = _unitOfWork.GetRepository<Report>();

            var reports = await reportRepo.GetListAsync(
                selector: r => _mapper.Map<ReportBasicDto>(r),
                predicate: r => r.CommonAreaObjectId == commonAreaObjectId,
                include: i => i.Include(r => r.User)
                              .Include(r => r.CommonAreaObject)
                                  .ThenInclude(cao => cao.CommonArea)
                                    .ThenInclude(x => x.Floor),
                orderBy: o => o.OrderByDescending(r => r.CreatedAt));

            // Cache for 30 minutes
            await _cacheService.SetAsync(cacheKey, reports, TimeSpan.FromMinutes(30));

            return reports;
        }

        public async Task<IPaginate<ReportDto>> GetMyReportsAsync(ReportFilterDto filterDto)
        {
            var userId = _userContext.CurrentUserId;
            filterDto.UserId = userId;
            return await GetPaginateReportsAsync(filterDto);
        }

        public async Task<string> ActivateReportAsync(int id)
        {
            var reportRepo = _unitOfWork.GetRepository<Report>();

            var report = await reportRepo.SingleOrDefaultAsync(
                predicate: r => r.ReportId == id);

            if (report == null)
            {
                throw new AppValidationException(
                    "Báo cáo không tồn tại.",
                    StatusCodes.Status404NotFound);
            }

            if (report.Status == ActiveStatus.Active)
            {
                throw new AppValidationException(
                    "Báo cáo đã ở trạng thái hoạt động.",
                    StatusCodes.Status400BadRequest);
            }

            report.Status = ActiveStatus.Active;
            reportRepo.UpdateAsync(report);
            await _unitOfWork.CommitAsync();

            // Clear cache after activate
            await _cacheService.RemoveAsync($"report:{id}");
            await _cacheService.RemoveByPrefixAsync("report:list");
            await _cacheService.RemoveByPrefixAsync("report:paginate");

            return "Kích hoạt báo cáo thành công.";
        }

        public async Task<string> DeactivateReportAsync(int id)
        {
            var reportRepo = _unitOfWork.GetRepository<Report>();

            var report = await reportRepo.SingleOrDefaultAsync(
                predicate: r => r.ReportId == id);

            if (report == null)
            {
                throw new AppValidationException(
                    "Báo cáo không tồn tại.",
                    StatusCodes.Status404NotFound);
            }

            if (report.Status == ActiveStatus.Inactive)
            {
                throw new AppValidationException(
                    "Báo cáo đã ở trạng thái ngưng hoạt động.",
                    StatusCodes.Status400BadRequest);
            }

            report.Status = ActiveStatus.Inactive;
            reportRepo.UpdateAsync(report);
            await _unitOfWork.CommitAsync();

            // Clear cache after deactivate
            await _cacheService.RemoveAsync($"report:{id}");
            await _cacheService.RemoveByPrefixAsync("report:list");
            await _cacheService.RemoveByPrefixAsync("report:paginate");

            return "Vô hiệu hóa báo cáo thành công.";
        }

        private Func<IQueryable<Report>, IOrderedQueryable<Report>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy))
                return q => q.OrderByDescending(r => r.ReportId);

            return sortBy.ToLower() switch
            {
                "id" => q => q.OrderBy(r => r.ReportId),
                "id_desc" => q => q.OrderByDescending(r => r.ReportId),
                "date" => q => q.OrderBy(r => r.CreatedAt),
                "date_desc" => q => q.OrderByDescending(r => r.CreatedAt),
                "title" => q => q.OrderBy(r => r.Title),
                "title_desc" => q => q.OrderByDescending(r => r.Title),
                _ => q => q.OrderByDescending(r => r.ReportId)
            };
        }
    }
}