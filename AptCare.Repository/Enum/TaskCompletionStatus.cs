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
        Completed = 2,    // Đã hoàn thành
        Failed = 3        // Thất bại (cần xử lý thêm)
    }
}
