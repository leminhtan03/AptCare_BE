using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Dtos.TechniqueDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.Account
{
    public class GetOwnProfileDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public List<MediaDto>? ProfileImage { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string CitizenshipIdentity { get; set; } = string.Empty;
        public DateTime? Birthday { get; set; }
        public List<ApartmentForUserProfileDto>? Apartments { get; set; }
        public List<TechniqueResponseDto>? Techniques { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; }
        public string? profileUrl { get; set; }
    }
}
