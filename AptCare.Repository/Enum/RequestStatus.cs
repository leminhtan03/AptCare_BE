using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Enum
{
    public enum RequestStatus
    {
        Pending = 1,
        Approved = 2,             // Đã duyệt, chờ bắt đầu
        InProgress = 3,           // Đang tiến hành (technician đã check-in)
        Scheduling = 4,            // Đã chẩn đoán (IR đã được approve)
        AcceptancePendingVerify = 5, // Chờ nghiệm thu
        Completed = 6,            // Hoàn tất
        Cancelled = 7,        // Bị từ chối
        WaitingManagerApproval = 8, // Chờ duyệt bởi Manager/TechLead
        Rejected = 9               // Bị từ chối sau khi chờ duyệt
    }
}
