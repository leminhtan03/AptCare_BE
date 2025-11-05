using AptCare.Repository.Enum.Apartment;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.BuildingDtos
{
    public class ApartmentUpdateDto
    {
        [Required(ErrorMessage = "ID tầng không được để trống.")]
        public int FloorId { get; set; }

        [Required(ErrorMessage = "Số phòng không được để trống.")]
        public string Room { get; set; } = null!;

        public string? Description { get; set; }
        public double Area { get; set; }
        public int Limit { get; set; }

        public ApartmentStatus Status { get; set; }
    }
    public class UpdateApartmentWithResidentDataDto 
    {
        public ICollection<ResidentOfApartmentDto>? Residents { get; set; }
    }
}
