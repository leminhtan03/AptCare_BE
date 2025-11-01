using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Enum
{
    public enum AppointmentStatus
    {
        Pending = 1, // Đã lên lịch
        Assigned = 2,  // Đã gán kỹ thuật viên
        Confirmed = 3,     // kỹ thuật viên xác đã được xác nhận ca làm
        InVisit = 4,      // Đang trong buổi khảo sát
        AwaitingIRApproval = 5,
        Visited = 6,              // Kết thúc buổi khảo sát (không sửa trong buổi này)

        // Nhánh sửa chữa (cùng buổi hoặc buổi khác)
        PreCheck = 7,           // Kiểm tra sơ bộ trước khi thi công (ở buổi sửa)
        InRepair = 8,             // Đang thi công
        OnHold = 9,               // Tạm dừng (đợi vật tư/không vào nhà/…)
        Completed = 10,            // Hoàn tất buổi (đã thi công xong)

        // Sự cố/lịch
        Rescheduled = 11,          // Đổi lịch
        Cancelled = 12,            // Hủy lịch
        NoShowCustomer = 13,       // Khách vắng
        NoShowTechnician = 14     // Kỹ thuật viên vắng
    }
}
