using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Enum.Apartment
{
    public enum RoleInApartmentType
    {
        Member,
        Owner
    }
    public enum RelationshipToOwnerType
    {
        Self,
        Family,
        Tenant,
        Guest
    }
    public enum ApartmentStatus
    {
        Active,
        Inactive,
        UnderMaintenance
    }


}
