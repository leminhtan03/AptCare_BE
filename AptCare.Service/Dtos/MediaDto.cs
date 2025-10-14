using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos
{
    public class MediaDto
    {
        public int MediaId { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public DateTime CreatedAt { get; set; }
        public ActiveStatus Status { get; set; }
    }
}

