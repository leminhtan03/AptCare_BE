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
        [Required]
        public string Entity { get; set; } = null!;
        [Required]
        public string FilePath { get; set; } = null!;
        [Required]
        public string FileName { get; set; } = null!;
        [Required]
        public string ContentType { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public ActiveStatus Status { get; set; }
    }
}
