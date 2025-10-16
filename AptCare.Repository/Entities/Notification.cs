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
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        public int? MessageId { get; set; }
        public int ReceiverId { get; set; }
        [Required]
        public string Description { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public NotificationType Type { get; set; }
        [ForeignKey("ReceiverId")]
        public Account Receiver { get; set; }
        [ForeignKey("MessageId")]
        public Message? Message { get; set; }
    }
}
