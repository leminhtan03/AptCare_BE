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
        [Key]
        public int UserApartmentId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }

        [ForeignKey("Apartment")]
        public int ApartmentId { get; set; }
        public Apartment Apartment { get; set; }

        public RoleInApartmentType RoleInApartment { get; set; }

        public RelationshipToOwnerType RelationshipToOwner { get; set; }

        public ActiveStatus Status { get; set; }
    }
}
