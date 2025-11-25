using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Entities
{
    public class Technique
    {
        [Key]
        public int TechniqueId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        public ICollection<Issue>? Issues { get; set; }
        public ICollection<TechnicianTechnique>? TechnicianTechniques { get; set; }
        public ICollection<MaintenanceSchedule>? MaintenanceSchedules { get; set; }
    }
}
