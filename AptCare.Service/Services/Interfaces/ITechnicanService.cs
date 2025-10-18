using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.TechniqueDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface ITechnicanService
    {
        Task<TechniqueListItemDto> CreateAsync(TechniqueCreateDto dto);
        Task<TechniqueListItemDto> UpdateAsync(int id, TechniqueUpdateDto dto);
        Task DeleteAsync(int id);
        Task<TechniqueListItemDto?> GetByIdAsync(int id);
        Task<IPaginate<TechniqueListItemDto>> ListAsync(PaginateDto q);
        Task<bool> ExistsAsync(int id);
    }
}
