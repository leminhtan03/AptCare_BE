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
        public User User { get; set; }

        [ForeignKey("CommonArea")]
        public int CommonAreaId { get; set; }
        public CommonArea CommonArea { get; set; }

        [Required]
        [MaxLength(256)]
        public string Title { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }
        //Thay đổi sau
        public ActiveStatus Status { get; set; }
    }
}
