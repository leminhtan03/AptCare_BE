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
        /// <summary>
        /// Mã số căn hộ (ví dụ: "A-101")
        /// </summary>
        public string RoomNumber { get; set; }

        /// <summary>
        /// Vai trò của người dùng trong căn hộ này (ví dụ: "Owner")
        /// </summary>
        public string RoleInApartment { get; set; }

        public string RelationshipToOwner { get; set; }
    }
    public class ApartmentForUserProfileDto : ApartmentForUserDto
    {
        public int? Floor { get; set; }
    }
}
