using Microsoft.AspNetCore.Http;
namespace AptCare.Service.Dtos.ContractDtos
{
    public class ContractUpdateDto
    {
        public string? ContractorName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? Amount { get; set; }
        public string? Description { get; set; }
        public IFormFile? ContractFile { get; set; }
    }
}
