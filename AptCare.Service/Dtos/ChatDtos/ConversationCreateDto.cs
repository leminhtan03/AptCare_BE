using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.ChatDtos
{
    public class ConversationCreateDto
    {
        public string? Title { get; set; }
        public List<int> UserIds { get; set; } = null!;
    }
}
