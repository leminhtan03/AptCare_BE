using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Entities
{
    public class TechnicianTechnique
    {
        [Required]
        [ForeignKey("Technician")]
        public int TechnicianId { get; set; }

        [Required]
        [ForeignKey("Technique")]
        public int TechniqueId { get; set; }

        public User Technician { get; set; } = null!;
        public Technique Technique { get; set; } = null!;
    }
}
