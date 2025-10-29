using AptCare.Service.Dtos.BuildingDtos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.UserDtos
{
    public class UpdateUserDto
    {
        [MaxLength(256, ErrorMessage = "Tên không được vượt quá 256 ký tự.")]
        public string? FirstName { get; set; }

        [MaxLength(256, ErrorMessage = "Họ không được vượt quá 256 ký tự.")]
        public string? LastName { get; set; }

        [MaxLength(50, ErrorMessage = "CCCD không được vượt quá 50 ký tự.")]
        public string? CitizenshipIdentity { get; set; }

        public DateTime? Birthday { get; set; }

        public string? Status { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [MaxLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự.")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [MaxLength(256, ErrorMessage = "Email không được vượt quá 256 ký tự.")]
        public string? Email { get; set; }
        //public List<ApartmentForUserDto>? Apartments { get; set; }

    }
}
