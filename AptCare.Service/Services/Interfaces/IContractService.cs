using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.ContractDtos;

namespace AptCare.Service.Services.Interfaces
{
    public interface IContractService
    {
        Task<ContractDto> CreateContractAsync(ContractCreateDto dto);
        Task<ContractDto> GetContractByIdAsync(int contractId);
        Task<IEnumerable<ContractDto>> GetContractsByRepairRequestIdAsync(int repairRequestId);
        Task<IPaginate<ContractDto>> GetPaginateContractsAsync(PaginateDto dto);
        Task<string> UpdateContractAsync(int contractId, ContractUpdateDto dto);
        Task<string> InactivateContractAsync(int contractId);
        Task<bool> CanCreateContractAsync(int repairRequestId);
    }
}