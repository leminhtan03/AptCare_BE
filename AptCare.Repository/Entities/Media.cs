using AptCare.Repository.Enum;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedAt { get; set; }
        public ActiveStatus Status { get; set; }
    }
}
