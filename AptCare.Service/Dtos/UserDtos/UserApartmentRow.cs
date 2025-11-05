using AptCare.Repository.Enum.Apartment;

namespace AptCare.Service.Dtos.UserDtos
{
    public sealed class UserApartmentRow
    {
        public int RowIndex { get; init; }
        public int UserId { get; init; }
        public int ApartmentId { get; init; }
        public RoleInApartmentType Role { get; init; }
        public string? RelationshipToOwner { get; init; }
    }
}
