using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Enum
{
    public enum MessageStatus
    {
        Sent = 1,
        Delivered = 2,
        Read = 3
    }

    public enum MessageType
    {
        Text = 1,
        Image = 2,
        File = 3,
        Video = 4,
        Audio = 5,
        System = 6,
        Emoji = 7,
        Location = 8
    }
}
