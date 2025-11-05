using System.ComponentModel.DataAnnotations;

namespace AptCare.Service.Dtos.BuildingDtos
{
    public class ApartmentCreateDto
    {
        [Required(ErrorMessage = "ID tầng không được để trống.")]
        public int FloorId { get; set; }

        [Required(ErrorMessage = "Số phòng không được để trống.")]
        public string Room { get; set; } = null!;

        public string? Description { get; set; }
        public double Area { get; set; }
        public int Limit { get; set; }
    }
}
