using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.IssueDto;
namespace AptCare.Service.Services.Interfaces
{
    public interface IIssueService
    {
        Task<IssueListItemDto> CreateAsync(IssueCreateDto dto);
        Task<string> UpdateAsync(int id, IssueUpdateDto dto);
        Task DeleteAsync(int id); // soft
        Task<IssueListItemDto?> GetByIdAsync(int id);
        Task<IPaginate<IssueListItemDto>> ListAsync(PaginateDto q, int? techniqueId = null);
    }
}
