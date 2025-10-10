using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.ChatDtos
{
    public class TextMessageCreateDto
    {
        [Required]
        public int ConversationId { get; set; }

        [Required]
        public string Content { get; set; } = null!;

        public int? RellyMessageId { get; set; }

    }
}
