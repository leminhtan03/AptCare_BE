using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Enum
{   
    public enum WorkSlotStatus
    {
        NotStarted = 1, // Chưa làm
        Working = 2,    // Đang làm
        Completed = 3,  // Đã làm
        Off = 4         // Nghỉ
    }
}
