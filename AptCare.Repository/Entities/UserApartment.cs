using AptCare.Repository.Enum;
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
    public class UserApartment
    {
        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        [Required]
        [ForeignKey("Apartment")]
        public int ApartmentId { get; set; }
        public Apartment Apartment { get; set; } = null!;

        public RoleInApartmentType RoleInApartment { get; set; }

        public string? RelationshipToOwner { get; set; }

        public ActiveStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DisableAt { get; set; }
    }
}
