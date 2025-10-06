using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Enum.Apartment
{
    public enum RoleInApartmentType
    {
        Member = 1,
        Owner = 2
    }
    public enum RelationshipToOwnerType
    {
        Self = 1,
        Family = 2,
        Tenant = 3,
        Guest = 4
    }
    public enum ApartmentStatus
    {
        Active = 1,
        Inactive = 2,
        UnderMaintenance = 3
    }


}
