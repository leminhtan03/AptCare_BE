using AptCare.Service.Dtos.Account;
using AptCare.Service.Dtos.BuildingDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.UserDtos
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string CitizenshipIdentity { get; set; } = null!;
        public DateTime? Birthday { get; set; }
        public List<ApartmentForUserDto>? Apartments { get; set; }
        public AccountForAdminDto? AccountInfo { get; set; }
        public string Status { get; set; } = null!;
    }

    public class UserBasicDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime? Birthday { get; set; }
    }
}
