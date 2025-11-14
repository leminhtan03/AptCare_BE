using Microsoft.AspNetCore.Http;

namespace AptCare.Service.Dtos.ContractDtos
{
    public class ContractCreateDto
    {
        public int RepairRequestId { get; set; }
        public string ContractorName { get; set; } = null!;
        public string ContractCode { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? Amount { get; set; }
        public string Description { get; set; } = null!;
        public IFormFile ContractFile { get; set; } = null!;
    }
}
