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
    public class Message
    {
        [Key]
        public int MessageId { get; set; }
        [ForeignKey("Conversation")]
        public int ConversationId { get; set; }
        [ForeignKey("Sender")]
        public int SenderId { get; set; }
        public User Sender { get; set; } = null!;
        [ForeignKey("RellyMessage")]
        public int? ReplyMessageId { get; set; }
        public string Content { get; set; }
        public MessageType Type { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedAt { get; set; }
        public MessageStatus Status { get; set; }

        public Message? ReplyMessage { get; set; }

        public Conversation Conversation { get; set; } = null!;
        public ICollection<Message>? ReplyMessages { get; set; }

        public Notification? Notification { get; set; }
    }
}
