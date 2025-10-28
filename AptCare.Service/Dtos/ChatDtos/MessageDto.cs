using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.ChatDtos
{
    public class MessageDto
    {
        public int MessageId { get; set; }
        public int ConversationId { get; set; }
        public string Slug { get; set; } = null!;
        public int SenderId { get; set; }
        public string SenderName { get; set; } = null!;
        public string? SenderAvatar { get; set; } // nếu có avatar người gửi

        public string? Content { get; set; }
        public string Type { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        // Nếu là reply message
        public int? ReplyMessageId { get; set; }
        public string? ReplyType { get; set; }
        public string? ReplyContent { get; set; }
        public string? ReplySenderName { get; set; }

        public bool IsMine { get; set; }
    }
}
