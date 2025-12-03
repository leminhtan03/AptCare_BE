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
    public class MaintenanceTask
    {
        [Key]
        public int MaintenanceTaskId { get; set; }

        [Required]
        [ForeignKey("CommonAreaObjectType")]
        public int CommonAreaObjectTypeId { get; set; }

        public CommonAreaObjectType CommonAreaObjectType { get; set; } = null!;

        [Required]
        [MaxLength(256)]
        public string TaskName { get; set; } = null!;

        [MaxLength(1000)]
        public string? TaskDescription { get; set; }

        [MaxLength(500)]
        public string? RequiredTools { get; set; }

        public int DisplayOrder { get; set; } 

        public double EstimatedDurationMinutes { get; set; }

        public ActiveStatus Status { get; set; }
        public ICollection<RepairRequestTask>? RepairRequestTasks { get; set; }
    }
}
