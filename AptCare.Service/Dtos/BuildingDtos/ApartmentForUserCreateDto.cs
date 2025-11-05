using AptCare.Repository.Enum.Apartment;
using System.ComponentModel.DataAnnotations;

namespace AptCare.Service.Dtos.BuildingDtos
{
    public class ApartmentForUserCreateDto
    {
        public int ApartmentId { get; set; }

        [Required(ErrorMessage = "Vai trò trong căn hộ không được để trống.")]
        public RoleInApartmentType RoleInApartment { get; set; }
        public string? RelationshipToOwner { get; set; }
    }
}
