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
        Available = 1,  // Trống, sẵn sàng nhận việc
        Booked = 2,     // Đã có lịch hẹn
        Completed = 3,  // Đã hoàn thành công việc trong ca
        Cancelled = 4  // Ca đã bị hủy
    }
}
