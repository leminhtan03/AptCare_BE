using AptCare.Repository.Enum;

namespace AptCare.Service.Dtos.ContractDtos
{
    public class ContractDto
    {
        public int ContractId { get; set; }
        public int? RepairRequestId { get; set; }
        public string ContractorName { get; set; } = null!;
        public string ContractCode { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? Amount { get; set; }
        public string Description { get; set; } = null!;
        public ActiveStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public MediaDto? ContractFile { get; set; }
    }
}
