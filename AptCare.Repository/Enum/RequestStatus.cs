using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Enum
{
    public enum RequestStatus
    {
        Pending = 1,              // Chờ duyệt
        Approved = 2,             // Đã duyệt, chờ bắt đầu
        InProgress = 3,           // Đang tiến hành (technician đã check-in)
        Diagnosed = 4,            // Đã chẩn đoán (IR đã được approve)
        CompletedPendingVerify = 5, // Hoàn tất, chờ kiểm duyệt
        AcceptancePendingVerify = 6, // Chờ nghiệm thu
        Completed = 7,            // Hoàn tất
        Cancelled = 8,            // Đã hủy
        Rejected = 9,            // Bị từ chối
        Rescheduling = 10        // cần xếp lịch
    }
}
