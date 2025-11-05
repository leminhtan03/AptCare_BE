using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Service.Dtos.BuildingDtos;

namespace AptCare.Service.Dtos.UserDtos
{
    public class CreateUserDto
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string CitizenshipIdentity { get; set; } = null!;
        public DateTime? Birthday { get; set; }
        public AccountRole Role { get; set; }
        public List<ApartmentForUserCreateDto>? Apartments { get; set; }
        public List<int>? TechniqueIds { get; set; }
        public bool CreateAccount { get; set; } = false;
    }

}
