using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Enum
{
    public enum AppointmentStatus
    {
        Pending = 1,           // Chờ phân công
        Assigned = 2,          // Đã phân công technician
        Confirmed = 3,         // Technician đã xác nhận
        InVisit = 4,           // Đang kiểm tra (đã check-in)
        AwaitingIRApproval = 5,// Chờ duyệt IR
        InRepair = 6,          // Đang thi công
        Completed = 7,        // Hoàn tất
        Cancelled = 8,   // Hủy

    }
}
