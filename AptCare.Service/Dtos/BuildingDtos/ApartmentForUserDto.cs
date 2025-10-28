using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.BuildingDtos
{
    public class ApartmentForUserDto
    {
        public int ApartmentId { get; set; }
        public string RoomNumber { get; set; }
        public string RoleInApartment { get; set; }

        public string RelationshipToOwner { get; set; }
    }
    public class ApartmentForUserProfileDto : ApartmentForUserDto
    {
        public int? Floor { get; set; }
    }
}
