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
        Visited = 6,           // Kết thúc kiểm tra (chưa sửa)
        PreCheck = 7,          // Kiểm tra trước khi thi công
        InRepair = 8,          // Đang thi công
        OnHold = 9,            // Tạm dừng
        Completed = 10,        // Hoàn tất
        Rescheduled = 11,      // Đổi lịch
        Cancelled = 12,        // Hủy
        NoShowCustomer = 13,   // Khách vắng
        NoShowTechnician = 14  // Technician vắng
    }
}
