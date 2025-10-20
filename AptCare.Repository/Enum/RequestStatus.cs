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
        InProgress = 3,        // Đang sửa chữa / trong quá trình thực hiện
        Completed = 4,         // Hoàn tất (đã có RepairReport)
        Rejected = 5,          // Bị từ chối / không được phê duyệt
        Cancelled = 6          // Bị hủy bởi cư dân hoặc quản lý
    }
}
