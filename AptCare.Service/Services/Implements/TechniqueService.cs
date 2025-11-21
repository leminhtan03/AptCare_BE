using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.TechniqueDto;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Implements
{
    public class TechniqueService : BaseService<TechniqueService>, ITechniqueService
    {
        private readonly IRedisCacheService _cacheService;

        public TechniqueService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork,
            ILogger<TechniqueService> logger,
            IMapper mapper,
            IRedisCacheService cacheService) : base(unitOfWork, logger, mapper)
        {
            _cacheService = cacheService;
        }

        public async Task<TechniqueListItemDto> CreateAsync(TechniqueCreateDto dto)
        {
            try
            {
                if (await _unitOfWork.GetRepository<Repository.Entities.Technique>().AnyAsync(t => t.Name == dto.Name))
                {
                    throw new ApplicationException("Technique với tên tương tự đã tồn tại");
                }
                var technique = _mapper.Map<Technique>(dto);
                await _unitOfWork.GetRepository<Technique>().InsertAsync(technique);
                await _unitOfWork.CommitAsync();

                // Clear cache after create
                await _cacheService.RemoveByPrefixAsync("technique");

                return _mapper.Map<TechniqueListItemDto>(technique);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating technique.");
                throw new ApplicationException("An error occurred while creating the technique.", ex);
            }
        }

        public async Task<TechniqueListItemDto?> GetByIdAsync(int id)
        {
            var cacheKey = $"technique:{id}";

            var cachedTechnique = await _cacheService.GetAsync<TechniqueListItemDto>(cacheKey);
            if (cachedTechnique != null)
            {
                return cachedTechnique;
            }

            var technique = await _unitOfWork.GetRepository<Repository.Entities.Technique>().SingleOrDefaultAsync(
                predicate: t => t.TechniqueId == id,
                selector: e => _mapper.Map<TechniqueListItemDto>(e),
                include: e => e.Include(e => e.Issues));

            if (technique == null)
                throw new ApplicationException("Technique không tồn tại");

            // Cache for 1 hour (techniques rarely change)
            await _cacheService.SetAsync(cacheKey, technique, TimeSpan.FromHours(1));

            return technique;
        }

        public async Task<IPaginate<TechniqueListItemDto>> ListAsync(PaginateDto dto)
        {
            try
            {
                int page = dto.page > 0 ? dto.page : 1;
                int size = dto.size > 0 ? dto.size : 10;
                string search = dto.search?.ToLower() ?? string.Empty;
                string filter = dto.filter?.ToLower() ?? string.Empty;
                string sortBy = dto.sortBy?.ToLower() ?? string.Empty;

                var cacheKey = $"technique:paginate:page:{page}:size:{size}:search:{search}:filter:{filter}:sort:{sortBy}";

                var cachedResult = await _cacheService.GetAsync<Paginate<TechniqueListItemDto>>(cacheKey);
                if (cachedResult != null)
                {
                    return cachedResult;
                }

                Expression<Func<Technique, bool>> predicate = p =>
                   (string.IsNullOrEmpty(search) || p.Name.Contains(search) ||
                                                    p.Name.Contains(search) ||
                                                    p.Description.Contains(search));
                var orderBy = BuildOrderBy(dto.sortBy);

                var result = await _unitOfWork.GetRepository<Technique>().GetPagingListAsync(
                    selector: x => _mapper.Map<TechniqueListItemDto>(x),
                    predicate: predicate,
                    include: e => e.Include(e => e.Issues),
                    orderBy: orderBy,
                    page: page,
                    size: size);

                // Cache for 30 minutes
                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while listing techniques.");
                throw new ApplicationException("An error occurred while listing the techniques.", ex);
            }
        }

        public async Task<TechniqueListItemDto> UpdateAsync(int id, TechniqueUpdateDto dto)
        {
            try
            {
                var technique = await _unitOfWork.GetRepository<Repository.Entities.Technique>().SingleOrDefaultAsync(predicate: t => t.TechniqueId == id);
                if (technique == null)
                    throw new ApplicationException("Technique không tồn tại");

                _mapper.Map(dto, technique);

                _unitOfWork.GetRepository<Technique>().UpdateAsync(technique);
                await _unitOfWork.CommitAsync();

                // Clear cache after update
                await _cacheService.RemoveAsync($"technique:{id}");
                await _cacheService.RemoveByPrefixAsync("technique:paginate");
                await _cacheService.RemoveByPrefixAsync("issue"); // Issues depend on technique

                return _mapper.Map<TechniqueListItemDto>(technique);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating technique.");
                throw new ApplicationException("An error occurred while updating the technique.", ex);
            }
        }

        private Func<IQueryable<Technique>, IOrderedQueryable<Technique>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy)) return q => q.OrderByDescending(p => p.TechniqueId);

            return sortBy.ToLower() switch
            {
                "issue_name" => q => q.OrderBy(p => p.Name),
                "issue_name_desc" => q => q.OrderByDescending(p => p.Name),
                _ => q => q.OrderByDescending(p => p.Name)
            };
        }
    }
}
