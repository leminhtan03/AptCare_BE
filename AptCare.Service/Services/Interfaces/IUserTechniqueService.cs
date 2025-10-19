using AptCare.Service.Dtos.TechniqueDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Interfaces
{
    public interface IUserTechniqueService
    {
        Task<string> CreateAsyns(AssignTechniqueFroTechnicanDto dto);
        Task<string> UpdateAsyns(UpdateTechniqueFroTechnicanDto dto);
        Task<ICollection<TechniqueResponseDto>> GetTechnicansTechniqueAsyns(int UserId);

    }
}
