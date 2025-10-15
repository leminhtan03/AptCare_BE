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
    public class Issue
    {
        [Key]
        public int IssueId { get; set; }

        [Required]
        [ForeignKey("Technique")]
        public int TechniqueId { get; set; }
        public Technique Technique { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsEmergency { get; set; }
        public int RequiredTechnician { get; set; } 
        public int EstimatedDuration { get; set; }
        public ActiveStatus Status { get; set; }

        public ICollection<RepairRequest>? RepairRequests { get; set; }
    }
}
