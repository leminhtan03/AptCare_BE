using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Entities
{
    public class Floor
    {
        [Key]
        public int FloorId { get; set; }

        [Required]
        [MaxLength(50)]
        public string BuildingCode { get; set; }

        public int FloorNumber { get; set; } // Đổi tên để tránh trùng với tên class

        public ActiveStatus Status { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        // Navigation properties
        public ICollection<Apartment>? Apartments { get; set; }
        public ICollection<CommonArea>? CommonAreas { get; set; }
    }
}
