using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.ContractDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AptCare.Service.Services.Interfaces.IS3File;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace AptCare.Service.Services.Implements
{
    public class ContractService : BaseService<ContractService>, IContractService
    {
        private readonly IS3FileService _s3FileService;
        private readonly IUserContext _userContext;
        private readonly IRedisCacheService _cacheService;

        public ContractService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<ContractService> logger,
            IMapper mapper,
            IS3FileService s3FileService,
            IUserContext userContext,
            IRedisCacheService cacheService) : base(unitOfWork, logger, mapper)
        {
            _s3FileService = s3FileService;
            _userContext = userContext;
            _cacheService = cacheService;
        }

        public async Task<ContractDto> CreateContractAsync(ContractCreateDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                var repairRequestRepo = _unitOfWork.GetRepository<RepairRequest>();
                var repairRequest = await repairRequestRepo.SingleOrDefaultAsync(
                    predicate: rr => rr.RepairRequestId == dto.RepairRequestId,
                    include: i => i.Include(rr => rr.Appointments)
                        .ThenInclude(a => a.InspectionReports)
                );
                if (repairRequest == null)
                    throw new AppValidationException("Yêu cầu sửa chữa không tồn tại.", StatusCodes.Status404NotFound);
                if (!await CanCreateContractAsync(dto.RepairRequestId))
                    throw new AppValidationException("Không thể tạo hợp đồng. Báo cáo kiểm tra gần nhất phải có giải pháp là 'Outsource' (Thuê ngoài).", StatusCodes.Status400BadRequest);

                var contractRepo = _unitOfWork.GetRepository<Contract>();
                if (await contractRepo.AnyAsync(predicate: c => c.ContractCode == dto.ContractCode))
                    throw new AppValidationException($"Mã hợp đồng '{dto.ContractCode}' đã tồn tại.", StatusCodes.Status400BadRequest);

                if (dto.ContractFile == null || dto.ContractFile.Length == 0)
                {
                    throw new AppValidationException("File hợp đồng không được để trống.");
                }

                if (!dto.ContractFile.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                {
                    throw new AppValidationException("File hợp đồng phải là định dạng PDF.");
                }

                var fileKey = await _s3FileService.UploadFileAsync(dto.ContractFile, $"contracts/{dto.RepairRequestId}/");

                if (string.IsNullOrEmpty(fileKey))
                {
                    throw new AppValidationException("Có lỗi xảy ra khi upload file hợp đồng.", StatusCodes.Status500InternalServerError);
                }
                var contract = _mapper.Map<Contract>(dto);
                await contractRepo.InsertAsync(contract);
                await _unitOfWork.CommitAsync();

                var mediaRepo = _unitOfWork.GetRepository<Media>();
                var media = new Media
                {
                    Entity = nameof(Contract),
                    EntityId = contract.ContractId,
                    FileName = dto.ContractFile.FileName,
                    FilePath = fileKey,
                    ContentType = dto.ContractFile.ContentType,
                    CreatedAt = DateTime.Now,
                    Status = ActiveStatus.Active
                };

                await mediaRepo.InsertAsync(media);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                // Clear cache after create
                await _cacheService.RemoveByPrefixAsync("contract");

                var result = _mapper.Map<ContractDto>(contract);
                result.ContractFile = _mapper.Map<MediaDto>(media);

                _logger.LogInformation(
                    "Created contract {ContractCode} for repair request {RepairRequestId}",
                    contract.ContractCode,
                    dto.RepairRequestId
                );

                return result;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error creating contract for repair request {RepairRequestId}", dto.RepairRequestId);
               throw new Exception(ex.Message);
            }
        }

        public async Task<bool> CanCreateContractAsync(int repairRequestId)
        {
            try
            {
                var inspectionReportRepo = _unitOfWork.GetRepository<InspectionReport>();

                var latestInspectionReport = await inspectionReportRepo.SingleOrDefaultAsync(
                    predicate: ir => ir.Appointment.RepairRequestId == repairRequestId
                        && ir.Status == ReportStatus.Approved,
                    orderBy: q => q.OrderByDescending(ir => ir.CreatedAt),
                    include: i => i.Include(ir => ir.Appointment)
                );

                if (latestInspectionReport == null)
                {
                    return false;
                }
                return latestInspectionReport.SolutionType == SolutionType.Outsource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if can create contract for repair request {RepairRequestId}", repairRequestId);
                return false;
            }
        }

        public async Task<ContractDto> GetContractByIdAsync(int contractId)
        {
            try
            {
                var cacheKey = $"contract:{contractId}";

                var cachedContract = await _cacheService.GetAsync<ContractDto>(cacheKey);
                if (cachedContract != null)
                {
                    return cachedContract;
                }

                var contractRepo = _unitOfWork.GetRepository<Contract>();
                var contract = await contractRepo.SingleOrDefaultAsync(
                    predicate: c => c.ContractId == contractId,
                    include: i => i.Include(c => c.RepairRequest)
                );

                if (contract == null)
                {
                    throw new AppValidationException(
                        "Hợp đồng không tồn tại.",
                        StatusCodes.Status404NotFound
                    );
                }
                var result = _mapper.Map<ContractDto>(contract);
                var media = await _unitOfWork.GetRepository<Media>().SingleOrDefaultAsync(
                    predicate: m => m.Entity == nameof(Contract)
                        && m.EntityId == contractId
                        && m.Status == ActiveStatus.Active
                );

                if (media != null)
                {
                    result.ContractFile = _mapper.Map<MediaDto>(media);
                }

                // Cache for 30 minutes
                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contract by id {ContractId}", contractId);
                throw new Exception(ex.Message);
            }
        }

        public async Task<IEnumerable<ContractDto>> GetContractsByRepairRequestIdAsync(int repairRequestId)
        {
            try
            {
                var cacheKey = $"contract:list:by_request:{repairRequestId}";

                var cachedResult = await _cacheService.GetAsync<IEnumerable<ContractDto>>(cacheKey);
                if (cachedResult != null)
                {
                    return cachedResult;
                }

                var contractRepo = _unitOfWork.GetRepository<Contract>();
                var contracts = await contractRepo.GetListAsync(
                    predicate: c => c.RepairRequestId == repairRequestId,
                    orderBy: q => q.OrderByDescending(c => c.CreatedAt)
                );

                var result = new List<ContractDto>();

                foreach (var contract in contracts)
                {
                    var dto = _mapper.Map<ContractDto>(contract);
                    var media = await _unitOfWork.GetRepository<Media>().SingleOrDefaultAsync(
                        predicate: m => m.Entity == nameof(Contract)
                            && m.EntityId == contract.ContractId
                            && m.Status == ActiveStatus.Active
                    );

                    if (media != null)
                    {
                        dto.ContractFile = _mapper.Map<MediaDto>(media);
                    }
                    result.Add(dto);
                }

                // Cache for 30 minutes
                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contracts by repair request id {RepairRequestId}", repairRequestId);
                throw new Exception(ex.Message);  
            }
        }

        public async Task<IPaginate<ContractDto>> GetPaginateContractsAsync(PaginateDto dto)
        {
            try
            {
                int page = dto.page > 0 ? dto.page : 1;
                int size = dto.size > 0 ? dto.size : 10;
                string search = dto.search?.ToLower() ?? string.Empty;
                string filter = dto.filter?.ToLower() ?? string.Empty;
                string sortBy = dto.sortBy?.ToLower() ?? string.Empty;

                var cacheKey = $"contract:paginate:page:{page}:size:{size}:search:{search}:filter:{filter}:sort:{sortBy}";

                var cachedResult = await _cacheService.GetAsync<Paginate<ContractDto>>(cacheKey);
                if (cachedResult != null)
                {
                    return cachedResult;
                }

                ActiveStatus? statusEnum = null;
                if (!string.IsNullOrEmpty(filter))
                {
                    if (Enum.TryParse<ActiveStatus>(filter, true, out var parsedStatus))
                    {
                        statusEnum = parsedStatus;
                    }
                }
                Expression<Func<Contract, bool>> predicate = c =>
                    (string.IsNullOrEmpty(search) ||
                        c.ContractCode.ToLower().Contains(search) ||
                        c.ContractorName.ToLower().Contains(search) ||
                        c.Description.ToLower().Contains(search)) &&
                    (statusEnum == null || c.Status == statusEnum.Value);

                var contractRepo = _unitOfWork.GetRepository<Contract>();
                var paginateResult = await contractRepo.GetPagingListAsync(
                    page: page,
                    size: size,
                    predicate: predicate,
                    include: i => i.Include(c => c.RepairRequest),
                    orderBy: BuildOrderBy(dto.sortBy ?? string.Empty),
                    selector: c => _mapper.Map<ContractDto>(c)
                );
                foreach (var item in paginateResult.Items)
                {
                    var media = await _unitOfWork.GetRepository<Media>().SingleOrDefaultAsync(
                        predicate: m => m.Entity == nameof(Contract)
                            && m.EntityId == item.ContractId
                            && m.Status == ActiveStatus.Active
                    );

                    if (media != null)
                    {
                        item.ContractFile = _mapper.Map<MediaDto>(media);
                    }
                }

                // Cache for 15 minutes
                await _cacheService.SetAsync(cacheKey, paginateResult, TimeSpan.FromMinutes(15));

                return paginateResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginate contracts");
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> UpdateContractAsync(int contractId, ContractUpdateDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var contractRepo = _unitOfWork.GetRepository<Contract>();
                var contract = await contractRepo.SingleOrDefaultAsync(
                    predicate: c => c.ContractId == contractId
                );

                if (contract == null)
                {
                    throw new AppValidationException(
                        "Hợp đồng không tồn tại.",
                        StatusCodes.Status404NotFound
                    );
                }

                if (contract.Status == ActiveStatus.Inactive)
                {
                    throw new AppValidationException(
                        "Không thể cập nhật hợp đồng đã bị vô hiệu hóa.",
                        StatusCodes.Status400BadRequest
                    );
                }
                if (!string.IsNullOrEmpty(dto.ContractorName))
                    contract.ContractorName = dto.ContractorName;

                if (dto.StartDate.HasValue)
                    contract.StartDate = dto.StartDate.Value;

                if (dto.EndDate.HasValue)
                    contract.EndDate = dto.EndDate.Value;

                if (dto.Amount.HasValue)
                    contract.Amount = dto.Amount.Value;

                if (!string.IsNullOrEmpty(dto.Description))
                    contract.Description = dto.Description;

                if (dto.ContractFile != null && dto.ContractFile.Length > 0)
                {
                    if (!dto.ContractFile.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                        throw new AppValidationException("File hợp đồng phải là định dạng PDF.");
                    var fileKey = await _s3FileService.UploadFileAsync(dto.ContractFile, $"contracts/{contract.RepairRequestId}/");

                    if (string.IsNullOrEmpty(fileKey))
                        throw new AppValidationException("Có lỗi xảy ra khi upload file hợp đồng.", StatusCodes.Status500InternalServerError);

                    var mediaRepo = _unitOfWork.GetRepository<Media>();
                    var oldMedia = await mediaRepo.SingleOrDefaultAsync(
                        predicate: m => m.Entity == nameof(Contract)
                            && m.EntityId == contractId
                            && m.Status == ActiveStatus.Active
                    );

                    if (oldMedia != null)
                    {
                        oldMedia.Status = ActiveStatus.Inactive;
                        mediaRepo.UpdateAsync(oldMedia);
                    }

                    var newMedia = new Media
                    {
                        Entity = nameof(Contract),
                        EntityId = contractId,
                        FileName = dto.ContractFile.FileName,
                        FilePath = fileKey,
                        ContentType = dto.ContractFile.ContentType,
                        CreatedAt = DateTime.Now,
                        Status = ActiveStatus.Active
                    };
                    await mediaRepo.InsertAsync(newMedia);
                }

                contractRepo.UpdateAsync(contract);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                // Clear cache after update
                await _cacheService.RemoveAsync($"contract:{contractId}");
                await _cacheService.RemoveByPrefixAsync("contract:list");
                await _cacheService.RemoveByPrefixAsync("contract:paginate");

                _logger.LogInformation("Updated contract {ContractId}", contractId);
                return "Cập nhật hợp đồng thành công.";
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error updating contract {ContractId}", contractId);
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> InactivateContractAsync(int contractId)
        {
            try
            {
                var contractRepo = _unitOfWork.GetRepository<Contract>();
                var contract = await contractRepo.SingleOrDefaultAsync(
                    predicate: c => c.ContractId == contractId
                );

                if (contract == null)
                    throw new AppValidationException("Hợp đồng không tồn tại.", StatusCodes.Status404NotFound);


                if (contract.Status == ActiveStatus.Inactive)
                    throw new AppValidationException("Hợp đồng đã bị vô hiệu hóa.", StatusCodes.Status400BadRequest);

                contract.Status = ActiveStatus.Inactive;
                contractRepo.UpdateAsync(contract);
                await _unitOfWork.CommitAsync();

                // Clear cache after inactivate
                await _cacheService.RemoveAsync($"contract:{contractId}");
                await _cacheService.RemoveByPrefixAsync("contract:list");
                await _cacheService.RemoveByPrefixAsync("contract:paginate");

                _logger.LogInformation("Inactivated contract {ContractId}", contractId);

                return "Vô hiệu hóa hợp đồng thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inactivating contract {ContractId}", contractId);
                throw new Exception(ex.Message);
            }
        }

        private Func<IQueryable<Contract>, IOrderedQueryable<Contract>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy))
                return q => q.OrderByDescending(c => c.CreatedAt);

            return sortBy.ToLower() switch
            {
                "id" => q => q.OrderBy(c => c.ContractId),
                "id_desc" => q => q.OrderByDescending(c => c.ContractId),
                "date" => q => q.OrderBy(c => c.CreatedAt),
                "date_desc" => q => q.OrderByDescending(c => c.CreatedAt),
                "code" => q => q.OrderBy(c => c.ContractCode),
                "code_desc" => q => q.OrderByDescending(c => c.ContractCode),
                _ => q => q.OrderByDescending(c => c.CreatedAt)
            };
        }
    }
}