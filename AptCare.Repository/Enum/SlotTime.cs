using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Enum
{
    public enum SlotTime
    {
        Slot1 = 1,    // 8:00 - 16:00
        Slot2 = 2,  // 16:00 - 24:00
        Slot3 = 3    // 00:00 - 8:00
    }
    public enum WorkSlotStatus
    {
        Available = 1,
        Absent = 2,

    }
}
