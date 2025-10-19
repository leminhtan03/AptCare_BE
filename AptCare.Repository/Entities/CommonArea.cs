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
    public class CommonArea
    {
        [Key]
        public int CommonAreaId { get; set; }

        [ForeignKey("Floor")]
        public int? FloorId { get; set; }
        public Floor? Floor { get; set; }

        [Required]
        [MaxLength(50)]
        public string AreaCode { get; set; } = null!;

        [Required]
        [MaxLength(256)]
        public string Name { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? Location { get; set; }

        public ActiveStatus Status { get; set; }
        public ICollection<Report>? Reports { get; set; }
        public ICollection<MaintenanceRequest>? MaintenanceRequests { get; set; }
        public ICollection<CommonAreaObject>? CommonAreaObjects { get; set; }
    }
}
