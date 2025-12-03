using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Entities
{
    public class RepairRequestTask
    {
        [Key]
        public int RepairRequestTaskId { get; set; }

        [Required]
        [ForeignKey("RepairRequest")]
        public int RepairRequestId { get; set; }

        public RepairRequest RepairRequest { get; set; } = null!;

        [ForeignKey("MaintenanceTaskTemplate")]
        public int? MaintenanceTaskTemplateId { get; set; } // Null nếu task được tạo thủ công

        public MaintenanceTask? MaintenanceTask { get; set; }

        [Required]
        [MaxLength(256)]
        public string TaskName { get; set; } = null!;

        [MaxLength(1000)]
        public string? TaskDescription { get; set; }

        public int DisplayOrder { get; set; }

        public TaskCompletionStatus Status { get; set; }

        [MaxLength(1000)]
        public string? TechnicianNote { get; set; }

        /// <summary>
        /// Kết quả kiểm tra: OK, Need Repair, Need Replacement
        /// </summary>
        [MaxLength(100)]
        public string? InspectionResult { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime? CompletedAt { get; set; }

        public int? CompletedByUserId { get; set; }

        [ForeignKey(nameof(CompletedByUserId))]
        public User? CompletedBy { get; set; }
    }
}
