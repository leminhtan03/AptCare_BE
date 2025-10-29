using AptCare.Repository.Enum.Apartment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.BuildingDtos
{
    public class ResidentOfApartmentDto
    {
        public int UserId { get; set; }
        public RoleInApartmentType RoleInApartment { get; set; }
        public string? RelationWithOwner { get; set; }


    }
}
