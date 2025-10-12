using AptCare.Repository.Enum;
using System;
using System.ComponentModel.DataAnnotations;

namespace AptCare.Repository.Entities
{
    public class Media
    {
        [Key]
        public int MediaId { get; set; }

        public int EntityId { get; set; }
        public string EntityType { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public DateTime CreatedAt { get; set; }
        public ActiveStatus Status { get; set; }
    }
}
