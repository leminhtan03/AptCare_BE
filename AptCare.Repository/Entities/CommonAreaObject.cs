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
    public class CommonAreaObject
    {
        [Key]
        public int CommonAreaObjectId { get; set; }

        [Required]
        [ForeignKey("CommonArea")]
        public int CommonAreaId { get; set; }
        public CommonArea CommonArea { get; set; } = null!;

        [Required]
        [MaxLength(256)]
        public string Name { get; set; } = null!;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public ActiveStatus Status { get; set; }
        public ICollection<Report>? Reports { get; set; }
        public MaintenanceSchedule? MaintenanceSchedule { get; set; }
    }
}
