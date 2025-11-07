using AptCare.Repository.Entities;
using AptCare.Repository.Enum.Apartment;
using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AptCare.Service.Dtos.UserDtos;

namespace AptCare.Service.Dtos.BuildingDtos
{
    public class ApartmentDto
    {
        public int ApartmentId { get; set; }
        public int FloorId { get; set; }
        public string Room { get; set; } = null!;
        public string Description { get; set; } = null!;
        public double Area { get; set; }
        public int Limit { get; set; }
        public string Status { get; set; } = null!;
        public string Floor { get; set; } = null!;
        public List<UserInApartmentDto>? Users { get; set; }
    }

    public class ApartmentBasicDto
    {
        public int ApartmentId { get; set; }
        public int FloorId { get; set; }
        public string Room { get; set; } = null!;
        public string Description { get; set; } = null!;
        public double Area { get; set; }
        public int Limit { get; set; }
        public string Status { get; set; } = null!;
        public string Floor { get; set; } = null!;
        public int UserCount { get; set; }
    }

    public class UserInApartmentDto
    {
        public UserDto User { get; set; } = null!;

        public RoleInApartmentType RoleInApartment { get; set; }

        public string RelationshipToOwner { get; set; } = null!;

        public ActiveStatus Status { get; set; }
    }
}
