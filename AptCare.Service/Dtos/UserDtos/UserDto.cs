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
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string CitizenshipIdentity { get; set; }
        public DateTime? Birthday { get; set; }
        public ApartmentDto UserApartment { get; set; } // < RoomNumber , roleinApartment>
        public AccountDto? Account { get; set; }
        public string Status { get; set; } // Hiển thị dưới dạng chuỗi
    }


}
