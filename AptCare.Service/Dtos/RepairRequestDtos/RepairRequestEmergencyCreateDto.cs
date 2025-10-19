using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.RepairRequestDtos
{
    public class RepairRequestEmergencyCreateDto
    {
        [Required]
        public int ApartmentId { get; set; }

        [Required]
        public int IssueId { get; set; }

        [Required]
        public string Object { get; set; } = null!;

        [Required]
        public string Description { get; set; } = null!;

        public List<IFormFile>? Files { get; set; }
    }
}
