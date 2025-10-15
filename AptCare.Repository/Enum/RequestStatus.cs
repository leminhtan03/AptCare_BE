using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Enum
{
    public enum RequestStatus
    {
        Pending = 1,           // Mới tạo, chưa được xử lý
        Approved = 2,          // Đã được ban quản lý phê duyệt
        Assigned = 3,          // Đã phân công kỹ thuật viên / tạo appointment đầu tiên
        InProgress = 4,        // Đang sửa chữa / trong quá trình thực hiện
        WaitingForInspection = 5, // Đang chờ kiểm tra sau sửa chữa (inspection)
        Completed = 6,         // Hoàn tất (đã có RepairReport)
        Rejected = 7,          // Bị từ chối / không được phê duyệt
        Cancelled = 8          // Bị hủy bởi cư dân hoặc quản lý
    }
}
