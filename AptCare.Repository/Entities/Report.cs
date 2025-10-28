using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Entities
{
    public class Report
    {
        [Key]
        public int ReportId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [ForeignKey("CommonAreaObject")]
        public int CommonAreaObjectId { get; set; }
        public CommonAreaObject CommonAreaObject { get; set; } = null!;

        [Required]
        [MaxLength(256)]
        public string Title { get; set; } = null!;

        [MaxLength(1000)]
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public ActiveStatus Status { get; set; }
    }
}
