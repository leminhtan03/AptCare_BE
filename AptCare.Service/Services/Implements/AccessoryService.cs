using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using AptCare.Service.Dtos.AccessoryDto;

namespace AptCare.Service.Services.Implements
{
    public class AccessoryService : BaseService<AccessoryService>, IAccessoryService
    {
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IRedisCacheService _cacheService;

        public AccessoryService(
        IUnitOfWork<AptCareSystemDBContext> unitOfWork,
        ILogger<AccessoryService> logger,
        ICloudinaryService cloudinaryService,
        IRedisCacheService cacheService,
        IMapper mapper) : base(unitOfWork, logger, mapper)
        {
            _cloudinaryService = cloudinaryService;
            _cacheService = cacheService;
        }

        public async Task<string> CreateAccessoryAsync(AccessoryCreateDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var isDup = await _unitOfWork.GetRepository<Accessory>().AnyAsync(
                    predicate: x => x.Name == dto.Name
                );
                if (isDup)
                    throw new AppValidationException("Vật tư đã tồn tại.");

                var accessory = _mapper.Map<Accessory>(dto);
                await _unitOfWork.GetRepository<Accessory>().InsertAsync(accessory);
                await _unitOfWork.CommitAsync();

                if (dto.Images != null && dto.Images.Any())
                {
                    foreach (var file in dto.Images)
                    {
                        if (file == null || file.Length == 0)
                            throw new AppValidationException("File không hợp lệ.");

                        var filePath = await _cloudinaryService.UploadImageAsync(file);
                        if (string.IsNullOrEmpty(filePath))
                            throw new AppValidationException("Có lỗi xảy ra khi gửi file.", StatusCodes.Status500InternalServerError);

                        await _unitOfWork.GetRepository<Media>().InsertAsync(new Media
                        {
                            Entity = nameof(Accessory),
                            EntityId = accessory.AccessoryId,
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
                await _cacheService.RemoveByPrefixAsync("accessory");

                return "Tạo vật tư thành công.";
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<string> UpdateAccessoryAsync(int id, AccessoryUpdateDto dto)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var accessory = await _unitOfWork.GetRepository<Accessory>().SingleOrDefaultAsync(
                    predicate: x => x.AccessoryId == id
                );
                if (accessory == null)
                    throw new AppValidationException("Vật tư không tồn tại.", StatusCodes.Status404NotFound);

                var isDup = await _unitOfWork.GetRepository<Accessory>().AnyAsync(
                    predicate: x => x.Name == dto.Name && x.AccessoryId != id
                );
                if (isDup)
                    throw new AppValidationException("Tên vật tư đã tồn tại.");

                _mapper.Map(dto, accessory);
                _unitOfWork.GetRepository<Accessory>().UpdateAsync(accessory);
                await _unitOfWork.CommitAsync();

                if (dto.NewImages != null && dto.NewImages.Any())
                {
                    foreach (var file in dto.NewImages)
                    {
                        if (file == null || file.Length == 0)
                            throw new AppValidationException("File không hợp lệ.");

                        var filePath = await _cloudinaryService.UploadImageAsync(file);
                        if (string.IsNullOrEmpty(filePath))
                            throw new AppValidationException("Có lỗi xảy ra khi gửi file.", StatusCodes.Status500InternalServerError);

                        await _unitOfWork.GetRepository<Media>().InsertAsync(new Media
                        {
                            Entity = nameof(Accessory),
                            EntityId = accessory.AccessoryId,
                            FileName = file.FileName,
                            FilePath = filePath,
                            ContentType = file.ContentType,
                            CreatedAt = DateTime.Now,
                            Status = ActiveStatus.Active
                        });
                    }
                }

                if (dto.RemoveMediaIds != null && dto.RemoveMediaIds.Any())
                {
                    foreach (var mediaId in dto.RemoveMediaIds)
                    {
                        var media = await _unitOfWork.GetRepository<Media>().SingleOrDefaultAsync(
                            predicate: x => x.MediaId == mediaId
                        );
                        if (media == null)
                            throw new AppValidationException("Không tìm thấy media.", StatusCodes.Status404NotFound);

                        _unitOfWork.GetRepository<Media>().DeleteAsync(media);
                    }
                }

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                // Clear cache after update
                await _cacheService.RemoveAsync($"accessory:{id}");
                await _cacheService.RemoveByPrefixAsync("accessory:list");
                await _cacheService.RemoveByPrefixAsync("accessory:paginate");

                return "Cập nhật vật tư thành công.";
            }
            catch (Exception e)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<string> DeleteAccessoryAsync(int id)
        {
            try
            {
                var accessory = await _unitOfWork.GetRepository<Accessory>().SingleOrDefaultAsync(
                    predicate: x => x.AccessoryId == id
                );
                if (accessory == null)
                    throw new AppValidationException("Vật tư không tồn tại.", StatusCodes.Status404NotFound);

                _unitOfWork.GetRepository<Accessory>().DeleteAsync(accessory);
                await _unitOfWork.CommitAsync();

                // Clear cache after delete
                await _cacheService.RemoveByPrefixAsync("accessory");

                return "Xóa vật tư thành công.";
            }
            catch (Exception e)
            {
                throw new AppValidationException($"Lỗi hệ thống: {e.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<AccessoryDto> GetAccessoryByIdAsync(int id)
        {
            var cacheKey = $"accessory:{id}";

            var cachedAccessory = await _cacheService.GetAsync<AccessoryDto>(cacheKey);
            if (cachedAccessory != null)
            {
                return cachedAccessory;
            }

            var accessory = await _unitOfWork.GetRepository<Accessory>().ProjectToSingleOrDefaultAsync<AccessoryDto>(
                configuration: _mapper.ConfigurationProvider,
                predicate: p => p.AccessoryId == id
            );
            if (accessory == null)
                throw new AppValidationException("Vật tư không tồn tại.", StatusCodes.Status404NotFound);

            var medias = await _unitOfWork.GetRepository<Media>().GetListAsync(
                selector: s => _mapper.Map<MediaDto>(s),
                predicate: x => x.Entity == nameof(Accessory) && x.EntityId == id && x.Status == ActiveStatus.Active
            );

            accessory.Images = medias.ToList();

            // Cache for 30 minutes
            await _cacheService.SetAsync(cacheKey, accessory, TimeSpan.FromMinutes(30));

            return accessory;
        }

        public async Task<IPaginate<AccessoryDto>> GetPaginateAccessoryAsync(PaginateDto dto)
        {
            int page = dto.page > 0 ? dto.page : 1;
            int size = dto.size > 0 ? dto.size : 10;
            string search = dto.search?.ToLower() ?? string.Empty;
            string filter = dto.filter?.ToLower() ?? string.Empty;
            string sortBy = dto.sortBy?.ToLower() ?? string.Empty;

            var cacheKey = $"accessory:paginate:page:{page}:size:{size}:search:{search}:filter:{filter}:sort:{sortBy}";

            var cachedResult = await _cacheService.GetAsync<Paginate<AccessoryDto>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            Expression<Func<Accessory, bool>> predicate = p =>
                (string.IsNullOrEmpty(search) || p.Name.ToLower().Contains(search) || (p.Descrption != null && p.Descrption.ToLower().Contains(search))) &&
                (string.IsNullOrEmpty(filter) || (filter == "active" && p.Status == ActiveStatus.Active) || (filter == "inactive" && p.Status == ActiveStatus.Inactive));

            var result = await _unitOfWork.GetRepository<Accessory>().ProjectToPagingListAsync<AccessoryDto>(
                configuration: _mapper.ConfigurationProvider,
                predicate: predicate,
                orderBy: BuildOrderBy(dto.sortBy),
                page: page,
                size: size
            );

            foreach (var item in result.Items)
            {
                var medias = await _unitOfWork.GetRepository<Media>().GetListAsync(
                    selector: s => _mapper.Map<MediaDto>(s),
                    predicate: x => x.Entity == nameof(Accessory) && x.EntityId == item.AccessoryId && x.Status == ActiveStatus.Active
                );

                item.Images = medias.ToList();
            }

            // Cache for 15 minutes
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));

            return result;
        }

        public async Task<IEnumerable<AccessoryDto>> GetAccessoriesAsync()
        {
            var cacheKey = "accessory:list:active";

            var cachedResult = await _cacheService.GetAsync<IEnumerable<AccessoryDto>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var result = await _unitOfWork.GetRepository<Accessory>().ProjectToListAsync<AccessoryDto>(
                configuration: _mapper.ConfigurationProvider,
                predicate: p => p.Status == ActiveStatus.Active,
                orderBy: o => o.OrderBy(x => x.Name)
            );

            foreach (var item in result)
            {
                var medias = await _unitOfWork.GetRepository<Media>().GetListAsync(
                    selector: s => _mapper.Map<MediaDto>(s),
                    predicate: x => x.Entity == nameof(Accessory) && x.EntityId == item.AccessoryId && x.Status == ActiveStatus.Active
                );

                item.Images = medias.ToList();
            }

            // Cache for 1 hour
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(1));

            return result;
        }

        private Func<IQueryable<Accessory>, IOrderedQueryable<Accessory>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy)) return q => q.OrderByDescending(p => p.AccessoryId);

            return sortBy.ToLower() switch
            {
                "name" => q => q.OrderBy(p => p.Name),
                "name_desc" => q => q.OrderByDescending(p => p.Name),
                "price" => q => q.OrderBy(p => p.Price),
                "price_desc" => q => q.OrderByDescending(p => p.Price),
                _ => q => q.OrderByDescending(p => p.AccessoryId)
            };
        }
    }
}
