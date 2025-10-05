using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Enum
{
    public enum SlotTime
    {
        Slot1,    // 8:00 - 16:00
        Slot2,  // 16:00 - 24:00
        Slot3     // 00:00 - 8:00
    }
    public enum WorkSlotStatus
    {
        Available,  // Trống, sẵn sàng nhận việc
        Booked,     // Đã có lịch hẹn
        Completed,  // Đã hoàn thành công việc trong ca
        Cancelled   // Ca đã bị hủy
    }
}
