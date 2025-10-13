using AptCare.Repository.Enum;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AptCare.Repository.Entities
{
    public class Contract
    {
        [Key]
        public int ContractId { get; set; }

        public int? RepairRequestId { get; set; }
        public string ContractorName { get; set; }
        public string ContractCode { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? Amount { get; set; }
        public string Description { get; set; }
        public ActiveStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(RepairRequestId))]
        public RepairRequest RepairRequest { get; set; }
    }
}
