using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Paginate;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.IssueDto;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Implements
{
    public class IssueService : BaseService<IssueService>, IIssueService
    {
        private readonly IRedisCacheService _cacheService;

        public IssueService(
            IUnitOfWork<AptCareSystemDBContext> unitOfWork, 
            ILogger<IssueService> logger, 
            IMapper mapper,
            IRedisCacheService cacheService) : base(unitOfWork, logger, mapper)
        {
            _cacheService = cacheService;
        }

        public async Task<IssueListItemDto> CreateAsync(IssueCreateDto dto)
        {
            if (await _unitOfWork.GetRepository<Issue>().AnyAsync(i => i.Name == dto.Name && i.TechniqueId == dto.TechniqueId))
            {
                throw new ApplicationException("Issue đã tồn tại");
            }
            if (!await _unitOfWork.GetRepository<Technique>().AnyAsync(t => t.TechniqueId == dto.TechniqueId))
            {
                throw new ApplicationException("Technique không tồn tại");
            }
            var newIssue = _mapper.Map<Issue>(dto);
            await _unitOfWork.GetRepository<Issue>().InsertAsync(newIssue);
            await _unitOfWork.CommitAsync();

            // Clear cache after create
            await _cacheService.RemoveByPrefixAsync("issue");
            await _cacheService.RemoveByPrefixAsync($"technique:{dto.TechniqueId}");

            return _mapper.Map<IssueListItemDto>(newIssue);
        }

        public async Task DeleteAsync(int id)
        {
            if (!await _unitOfWork.GetRepository<Issue>().AnyAsync(predicate: i => i.IssueId == id))
            {
                throw new ApplicationException("Issue không tồn tại");
            }
            var issue = await _unitOfWork.GetRepository<Issue>().SingleOrDefaultAsync(predicate: i => i.IssueId == id);
            issue!.Status = Repository.Enum.ActiveStatus.Inactive;
            _unitOfWork.GetRepository<Issue>().UpdateAsync(issue);
            await _unitOfWork.CommitAsync();

            // Clear cache after delete
            await _cacheService.RemoveByPrefixAsync("issue");
            await _cacheService.RemoveByPrefixAsync($"technique:{issue.TechniqueId}");
        }

        public async Task<IssueListItemDto?> GetByIdAsync(int id)
        {
            var cacheKey = $"issue:{id}";

            var cachedIssue = await _cacheService.GetAsync<IssueListItemDto>(cacheKey);
            if (cachedIssue != null)
            {
                return cachedIssue;
            }

            if (!await _unitOfWork.GetRepository<Issue>().AnyAsync(predicate: i => i.IssueId == id))
            {
                throw new ApplicationException("Issue không tồn tại");
            }
            var issue = await _unitOfWork.GetRepository<Issue>().SingleOrDefaultAsync(predicate: i => i.IssueId == id);
            var result = _mapper.Map<IssueListItemDto>(issue);

            // Cache for 1 hour (issues rarely change)
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(1));

            return result;
        }

        public async Task<IPaginate<IssueListItemDto>> ListAsync(PaginateDto dto, int? techniqueId = null)
        {
            int page = dto.page > 0 ? dto.page : 1;
            int size = dto.size > 0 ? dto.size : 10;
            string search = dto.search?.ToLower() ?? string.Empty;
            string filter = dto.filter?.ToLower() ?? string.Empty;
            string sortBy = dto.sortBy?.ToLower() ?? string.Empty;

            var cacheKey = $"issue:paginate:page:{page}:size:{size}:search:{search}:filter:{filter}:technique:{techniqueId}:sort:{sortBy}";

            var cachedResult = await _cacheService.GetAsync<IPaginate<IssueListItemDto>>(cacheKey);
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

            Expression<Func<Issue, bool>> predicate = p =>
               (string.IsNullOrEmpty(search) || p.Name.Contains(search) ||
                                                p.Name.Contains(search) ||
                                                p.Description.Contains(search)) &&
               (string.IsNullOrEmpty(filter) || filterStatus == p.Status) &&
               (techniqueId == null || p.TechniqueId == techniqueId);

            var result = await _unitOfWork.GetRepository<Issue>().GetPagingListAsync(
                selector: x => _mapper.Map<IssueListItemDto>(x),
                predicate: predicate,
                orderBy: BuildOrderBy(dto.sortBy),
                page: page,
                size: size
                );

            // Cache for 30 minutes
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30));

            return result;
        }

        public async Task<string> UpdateAsync(int id, IssueUpdateDto dto)
        {
            try
            {
                var issue = _unitOfWork.GetRepository<Issue>().SingleOrDefaultAsync(predicate: i => i.IssueId == id).Result;
                if (issue == null)
                {
                    throw new ApplicationException("Issue không tồn tại");
                }
                if (!await _unitOfWork.GetRepository<Technique>().AnyAsync(predicate: i => i.TechniqueId == dto.TechniqueId))
                    throw new ApplicationException("Technique không tồn tại");
                
                _mapper.Map(dto, issue);
                _unitOfWork.GetRepository<Issue>().UpdateAsync(issue);
                await _unitOfWork.CommitAsync();

                // Clear cache after update
                await _cacheService.RemoveAsync($"issue:{id}");
                await _cacheService.RemoveByPrefixAsync("issue:paginate");
                await _cacheService.RemoveByPrefixAsync($"technique:{dto.TechniqueId}");

                return "Cập nhật Issue thành công";
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Issue không tồn tại");
            }
        }

        private Func<IQueryable<Issue>, IOrderedQueryable<Issue>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy)) return q => q.OrderByDescending(p => p.IssueId);

            return sortBy.ToLower() switch
            {
                "issue_name" => q => q.OrderBy(p => p.Name),
                "issue_name_desc" => q => q.OrderByDescending(p => p.Name),
                _ => q => q.OrderByDescending(p => p.Name)
            };
        }
    }
}
