using AptCare.Repository;
using AptCare.Repository.Entities;
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
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Implements
{
    public class IssueService : BaseService<IssueService>, IIssueService
    {
        public IssueService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<IssueService> logger, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
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
        }

        public async Task<IssueListItemDto?> GetByIdAsync(int id)
        {
            if (!await _unitOfWork.GetRepository<Issue>().AnyAsync(predicate: i => i.IssueId == id))
            {
                throw new ApplicationException("Issue không tồn tại");
            }
            var issue = await _unitOfWork.GetRepository<Issue>().SingleOrDefaultAsync(predicate: i => i.IssueId == id);
            return _mapper.Map<IssueListItemDto>(issue);
        }

        public Task<IPaginate<IssueListItemDto>> ListAsync(PaginateDto q, int? techniqueId = null)
        {
            throw new NotImplementedException();
        }

        public Task<IssueListItemDto> UpdateAsync(int id, IssueUpdateDto dto)
        {
            throw new NotImplementedException();
        }
        private Func<IQueryable<Issue>, IOrderedQueryable<Issue>> BuildOrderBy(string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy)) return q => q.OrderByDescending(p => p.IssueId);

            return sortBy.ToLower() switch
            {
                "issue_name" => q => q.OrderBy(p => p.Name),
                "issue_name_desc" => q => q.OrderByDescending(p => p.Name),
                _ => q => q.OrderByDescending(p => p.Name) // Default sort
            };
        }
    }
}
