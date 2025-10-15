using AptCare.Repository.Enum.Apartment;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Entities
{
    public class Apartment
    {
        [Key]
        public int ApartmentId { get; set; }

        [ForeignKey("Floor")]
        public int FloorId { get; set; }
        public Floor Floor { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string RoomNumber { get; set; } = null!;

        public ApartmentStatus Status { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        // Navigation properties
        public ICollection<UserApartment>? UserApartments { get; set; }
        public ICollection<RepairRequest>? RepairRequests { get; set; }
    }
}
