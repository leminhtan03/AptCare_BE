using AptCare.Repository.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Dtos.NotificationDtos
{
    public class NotificationCreateDto
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public NotificationType Type { get; set; }
    }

    public class NotificationPushRequestDto : NotificationCreateDto
    {
        public string? Image { get; set; }
        public IEnumerable<int> UserIds { get; set; } = null!;
    }
}
