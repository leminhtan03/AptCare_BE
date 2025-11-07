using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Enum
{
    public enum RequestStatus
    {
        Pending = 10,
        Approved = 20,             // Đã duyệt, chờ bắt đầu
        InProgress = 30,           // Đang tiến hành (technician đã check-in)
        Scheduling = 35,            // Đã chẩn đoán (IR đã được approve)
        AcceptancePendingVerify = 40, // Chờ nghiệm thu
        Completed = 50,            // Hoàn tất
        Cancelled = 60,        // Bị từ chối
    }
}
