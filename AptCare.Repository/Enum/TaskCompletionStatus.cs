using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Repository.Enum
{
    public enum TaskCompletionStatus
    {
        Pending = 1,      // Chưa thực hiện
        InProgress = 2,   // Đang thực hiện
        Completed = 3,    // Đã hoàn thành
        Skipped = 4,      // Bỏ qua (với lý do)
        Failed = 5        // Thất bại (cần xử lý thêm)
    }
}
