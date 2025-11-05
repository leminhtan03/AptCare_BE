using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.Account;
using AptCare.Service.Dtos.UserDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class AccountManageController : BaseApiController
    {

        private readonly IAccountService _accountService;
        public AccountManageController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        /// <summary>
        /// Lấy danh sách dữ liệu người dùng hệ thống theo trang với khả năng lọc và tìm kiếm. Nó sẽ lấy tất cả thông tin của nhân viên và quản lý hệ thống bao gồm dữ liệu cá nhân, vai trò và trạng thái tài khoản. (Ko có tất cả thông tin của cư dân, chỉ có các thông tin đã liên kết tk)
        /// </summary>
        /// <remarks>
        /// <para>Endpoint này trả về danh sách người dùng hệ thống (nhân viên, quản lý) được phân trang với các tùy chọn lọc và tìm kiếm.</para>
        /// <para>Hỗ trợ tìm kiếm theo tên, email, số điện thoại và lọc theo vai trò, trạng thái người dùng.</para>
        /// <para><strong>Lưu ý:</strong> Endpoint này yêu cầu quyền Manager.</para>
        /// 
        /// <para><strong>Ví dụ query parameters:</strong></para>
        /// <code>
        /// GET /api/usermanagement/system_users?searchQuery=Admin&role=Manager&status=Active&page=1&pageSize=10
        /// </code>
        /// </remarks>
        /// <param name="getSystemUserPageDto">Đối tượng chứa các tham số lọc và phân trang.
        /// <para><strong>Các thuộc tính bao gồm:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>SearchQuery:</strong> Từ khóa tìm kiếm (tìm trong tên, email, số điện thoại) - tùy chọn</description></item>
        /// <item><description><strong>Role:</strong> Vai trò người dùng để lọc (Enum: "Manager", "Resident", "Receptionist", "Technician","TechnicianLead") - tùy chọn</description></item>
        /// <item><description><strong>Status:</strong> Trạng thái người dùng để lọc (Enum: "Active", "Inactive") - tùy chọn</description></item>
        /// <item><description><strong>Page:</strong> Số trang hiện tại (mặc định: 1, tối thiểu: 1)</description></item>
        /// <item><description><strong>PageSize:</strong> Số lượng bản ghi trên mỗi trang (mặc định: 10, tối đa: 100)</description></item>
        /// </list>
        /// </param>
        /// <returns>
        /// <para><strong>Các trường hợp trả về:</strong></para>
        /// <list type="table">
        /// <item><term>200 OK</term><description>Trả về IPaginate&lt;UserDto&gt; chứa:
        ///   <list type="bullet">
        ///   <item><description><strong>Items:</strong> Danh sách UserDto của người dùng hệ thống</description></item>
        ///   <item><description><strong>Page:</strong> Trang hiện tại</description></item>
        ///   <item><description><strong>Size:</strong> Kích thước trang</description></item>
        ///   <item><description><strong>Total:</strong> Tổng số bản ghi</description></item>
        ///   <item><description><strong>TotalPages:</strong> Tổng số trang</description></item>
        ///   </list>
        /// </description></item>
        /// <item><term>400 Bad Request</term><description>Tham số đầu vào không hợp lệ (page < 1, pageSize > 100)</description></item>
        /// <item><term>500 Internal Server Error</term><description>Lỗi hệ thống trong quá trình xử lý</description></item>
        /// </list>
        /// </returns>
        /// <exception cref="ArgumentException">Ném khi các tham số phân trang không hợp lệ</exception>
        /// <exception cref="InvalidOperationException">Ném khi có lỗi trong quá trình truy vấn dữ liệu</exception>
        [HttpGet("system_users")]
        [ProducesResponseType(typeof(IPaginate<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GetSystemUserPage([FromQuery] GetSystemUserFilterDto getSystemUserPageDto)
        {
            var result = await _accountService.GetSystemUserPageAsync(getSystemUserPageDto.SearchQuery, getSystemUserPageDto.Role, getSystemUserPageDto.Status, getSystemUserPageDto.Page, getSystemUserPageDto.PageSize);
            return Ok(result);
        }
        /// <summary>
        /// Tạo tài khoản cho user đã tồn tại (chủ yếu dành cho Resident chưa có account).
        /// </summary>
        /// <remarks>
        /// <para><strong>⚠️ LƯU Ý QUAN TRỌNG:</strong></para>
        /// <para>Endpoint này CHỦ YẾU dành cho việc tạo account cho <strong>Resident</strong> 
        /// đã có UserData nhưng chưa có account (CreateAccount = false khi tạo UserData).</para>
        /// 
        /// <para><strong>Đối với Staff roles (Technician/Manager/etc):</strong></para>
        /// <list type="bullet">
        /// <item><description>Account đã được tạo TỰ ĐỘNG khi tạo UserData</description></item>
        /// <item><description>KHÔNG cần gọi endpoint này</description></item>
        /// <item><description>Nếu gọi sẽ báo lỗi "Người dùng đã có tài khoản"</description></item>
        /// </list>
        /// 
        /// <para><strong>Quy tắc xác định Role tự động:</strong></para>
        /// <list type="number">
        /// <item><description><strong>Resident:</strong> User có apartment (UserApartments)</description></item>
        /// </list>
        /// </remarks>
        [HttpPost("create_account/{userid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> CreateAccountForUser(int userid)
        {
            var result = await _accountService.CreateAccountForUserAsync(userid);
            return Ok(result);
        }
        /// <summary>
        /// Bật/tắt trạng thái tài khoản người dùng trong hệ thống.
        /// </summary>
        /// <remarks>
        /// <para>Endpoint này cho phép kích hoạt hoặc vô hiệu hóa tài khoản người dùng.</para>
        /// <para>Khi tài khoản đang Active sẽ chuyển sang Inactive và ngược lại.</para>
        /// <para><strong>Lưu ý:</strong> Endpoint này yêu cầu quyền Manager.</para>
        /// 
        /// <para><strong>Ví dụ:</strong></para>
        /// <code>
        /// PUT /api/accountmanage/toggle_account_status/123
        /// </code>
        /// </remarks>
        /// <param name="accountId">ID của tài khoản cần thay đổi trạng thái - bắt buộc</param>
        /// <returns>
        /// <para><strong>Các trường hợp trả về:</strong></para>
        /// <list type="table">
        /// <item><term>200 OK</term><description>Thay đổi trạng thái tài khoản thành công, trả về thông báo xác nhận</description></item>
        /// <item><term>400 Bad Request</term><description>AccountId không hợp lệ hoặc tài khoản không tồn tại</description></item>
        /// <item><term>401 Unauthorized</term><description>Người dùng chưa đăng nhập</description></item>
        /// <item><term>403 Forbidden</term><description>Người dùng không có quyền thực hiện chức năng này</description></item>
        /// <item><term>500 Internal Server Error</term><description>Lỗi hệ thống trong quá trình xử lý</description></item>
        /// </list>
        /// </returns>
        /// <exception cref="ArgumentException">Ném khi AccountId không hợp lệ</exception>
        /// <exception cref="InvalidOperationException">Ném khi có lỗi trong quá trình cập nhật trạng thái</exception>
        [HttpPut("toggle_account_status/{accountId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> TogleAccontStatus([FromRoute] int accountId)
        {
            var result = await _accountService.TogleAccontStatus(accountId);
            return Ok(result);
        }
    }
}
