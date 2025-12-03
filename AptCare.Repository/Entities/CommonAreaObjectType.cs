using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Entities
{
    public class CommonAreaObjectType
    {
        [Key]
        public int CommonAreaObjectTypeId { get; set; }

        [Required]
        [MaxLength(100)]
        public string TypeName { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        public ActiveStatus Status { get; set; }

        // Navigation properties
        public ICollection<CommonAreaObject>? CommonAreaObjects { get; set; }
        public ICollection<MaintenanceTask>? MaintenanceTasks { get; set; }
    }
}
