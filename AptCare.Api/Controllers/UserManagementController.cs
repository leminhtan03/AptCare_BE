using AptCare.Repository.Entities;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.UserDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AptCare.Api.Controllers
{
    public class UserManagementController : BaseApiController
    {
        private readonly IUserService _userService;
        public UserManagementController(IUserService userService)
        {
            _userService = userService;
        }
        /// <summary>
        /// Retrieves a user by their unique identifier.
        /// </summary>
        /// <remarks>
        /// This endpoint fetches detailed user information including personal data, apartment assignments, and account status.
        /// Returns 404 if the user is not found, or 500 if an internal error occurs.
        /// </remarks>
        /// <param name="id">The unique identifier of the user to retrieve.</param>
        /// <returns>
        /// Returns an ActionResult containing:
        /// - 200 OK with UserDto object if user is found
        /// - 404 Not Found if user doesn't exist
        /// - 500 Internal Server Error if an exception occurs
        /// </returns>
        //[Authorize(Roles = nameof(AccountRole.Manager))]
        [HttpGet("{id}")]
        public async Task<ActionResult> GetUserById(int id)
        {
            try
            {
                var result = await _userService.GetUserByIdAsync(id);
                if (result == null)
                {
                    return NotFound();
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing your request." + ex.Message);
            }
        }
        /// <summary>
        /// Cập nhật thông tin người dùng theo ID được chỉ định.
        /// </summary>
        /// <remarks>
        /// <para>Endpoint này cho phép cập nhật thông tin người dùng bao gồm dữ liệu cá nhân, phân công căn hộ và trạng thái.</para>
        /// <para>Chỉ những thuộc tính không null trong UpdateUserDto mới được cập nhật (partial update).</para>
        /// <para><strong>Lưu ý:</strong> Endpoint này yêu cầu quyền Manager .</para>
        /// 
        /// <para><strong>Ví dụ request body:</strong></para>
        /// <code>
        /// {
        ///   "firstName": "Nguyen Van",
        ///   "lastName": "A", 
        ///   "citizenshipIdentity": "123456789012",
        ///   "birthday": "1990-01-01",
        ///   "status": "Active",
        ///   "userApartments": [{
        ///     "roomNumber": "A-101",
        ///     "roleInApartment": "Owner",
        ///     "relationshipToOwner": "Self"
        ///   }]
        /// }
        /// </code>
        /// </remarks>
        /// <param name="id">ID duy nhất của người dùng cần cập nhật (phải là số nguyên dương).</param>
        /// <param name="updateUserDto">Đối tượng chứa thông tin cần cập nhật.
        /// <para><strong>Các thuộc tính bao gồm:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>FirstName:</strong> Tên (tối đa 256 ký tự, có validation)</description></item>
        /// <item><description><strong>LastName:</strong> Họ (tối đa 256 ký tự, có validation)</description></item>
        /// <item><description><strong>CitizenshipIdentity:</strong> Số CCCD/CMND (tối đa 50 ký tự)</description></item>
        /// <item><description><strong>Birthday:</strong> Ngày sinh (định dạng DateTime)</description></item>
        /// <item><description><strong>Status:</strong> Trạng thái người dùng</description></item>
        /// <item><description><strong>UserApartments:</strong> Danh sách căn hộ được phân công:
        ///   <list type="bullet">
        ///   <item><description><strong>RoomNumber:</strong> Số căn hộ (VD: "A-101")</description></item>
        ///   <item><description><strong>RoleInApartment:</strong> Vai trò - "Owner" (chủ hộ) hoặc "Member" (thành viên)</description></item>
        ///   <item><description><strong>RelationshipToOwner:</strong> Mối quan hệ với chủ hộ (VD: "Spouse", "Child", "Parent", "Self")</description></item>
        ///   </list>
        /// </description></item>
        /// </list>
        /// </param>
        /// <returns>
        /// <para><strong>Các trường hợp trả về:</strong></para>
        /// <list type="table">
        /// <item><term>200 OK</term><description>Cập nhật thành công, trả về UserDto đã được cập nhật</description></item>
        /// <item><term>404 Not Found</term><description>Không tìm thấy người dùng với ID được chỉ định</description></item>
        /// <item><term>400 Bad Request</term><description>Dữ liệu đầu vào không hợp lệ (model validation failed)</description></item>
        /// <item><term>500 Internal Server Error</term><description>Lỗi hệ thống trong quá trình xử lý</description></item>
        /// </list>
        /// </returns>
        /// <exception cref="ArgumentException">Ném khi ID không hợp lệ</exception>
        /// <exception cref="ValidationException">Ném khi dữ liệu đầu vào vi phạm validation rules</exception>
        //[Authorize(Roles = nameof(AccountRole.Manager))]
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            try
            {
                var result = await _userService.UpdateUserAsync(id, updateUserDto);
                if (result == null)
                {
                    return NotFound();
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing your request." + ex.Message);
            }
        }
        /// <summary>
        /// Lấy danh sách dữ liệu cư dân theo trang với khả năng lọc và tìm kiếm. Nó sẽ lấy tất cả thông tin của cư dân bao gồm dữ liệu cá nhân, căn hộ được phân công và trạng thái tài khoản. (Ko có các thông tin của nhân viên)
        /// </summary>
        /// <remarks>
        /// <para>Endpoint này trả về danh sách cư dân được phân trang với các tùy chọn lọc và tìm kiếm.</para>
        /// <para>Hỗ trợ tìm kiếm theo tên, email, số điện thoại và lọc theo trạng thái người dùng.</para>
        /// <para><strong>Lưu ý:</strong> Endpoint này yêu cầu quyền Manager.</para>
        /// 
        /// <para><strong>Ví dụ query parameters:</strong></para>
        /// <code>
        /// GET /api/usermanagement/residents_data?searchQuery=Nguyen&status=Active&page=1&pageSize=10
        /// </code>
        /// </remarks>
        /// <param name="getResidentDataFilterDto">Đối tượng chứa các tham số lọc và phân trang.
        /// <para><strong>Các thuộc tính bao gồm:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>SearchQuery:</strong> Từ khóa tìm kiếm (tìm trong tên, email, số điện thoại) - tùy chọn</description></item>
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
        ///   <item><description><strong>Items:</strong> Danh sách UserDto của cư dân</description></item>
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
        //[Authorize(Roles = nameof(AccountRole.Manager))]
        [HttpGet("residents_data")]
        public async Task<ActionResult> GetResidentDataPage([FromQuery] GetResidentDataFilterDto getResidentDataFilterDto)
        {
            try
            {
                var result = await _userService.GetReSidentDataPageAsync(getResidentDataFilterDto.SearchQuery, getResidentDataFilterDto.Status, getResidentDataFilterDto.Page, getResidentDataFilterDto.PageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing your request." + ex.Message);
            }
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
        //[Authorize(Roles = nameof(AccountRole.Manager))]
        [HttpGet("system_users")]
        public async Task<ActionResult> GetSystemUserPage([FromQuery] GetSystemUserFilterDto getSystemUserPageDto)
        {
            try
            {
                var result = await _userService.GetSystemUserPageAsync(getSystemUserPageDto.SearchQuery, getSystemUserPageDto.Role, getSystemUserPageDto.Status, getSystemUserPageDto.Page, getSystemUserPageDto.PageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing your request." + ex.Message);
            }
        }
        /// <summary>
        /// Nhập danh sách cư dân từ file Excel với validation và xử lý lỗi chi tiết.
        /// </summary>
        /// <remarks>
        /// <para>Endpoint này cho phép nhập hàng loạt thông tin cư dân từ file Excel (.xlsx).</para>
        /// <para>Hệ thống sẽ validate từng dòng dữ liệu và báo cáo chi tiết kết quả import bao gồm số dòng thành công, thất bại và lỗi cụ thể.</para>
        /// <para><strong>Lưu ý:</strong> Endpoint này yêu cầu quyền Manager.</para>
        /// <para><strong>Yêu cầu định dạng file Excel:</strong></para>
        /// <list type="bullet">
        /// <item><description>Định dạng: .xlsx (Excel 2007 trở lên)</description></item>
        /// <item><description>Kích thước tối đa: Theo cấu hình server</description></item>
        /// </list>
        /// <para><strong>Quy trình xử lý:</strong></para>
        /// <list type="number">
        /// <item><description>Kiểm tra định dạng file (.xlsx)</description></item>
        /// <item><description>Đọc và parse dữ liệu từ Excel</description></item>
        /// <item><description>Validate từng dòng dữ liệu theo business rules</description></item>
        /// <item><description>Tạo user và phân công căn hộ cho các dòng hợp lệ</description></item>
        /// <item><description>Trả về báo cáo chi tiết kết quả import</description></item>
        /// </list>
        /// </remarks>
        /// <param name="file">File Excel chứa danh sách cư dân cần import.
        /// </param>
        /// <returns>
        /// <para><strong>Các trường hợp trả về:</strong></para>
        /// <list type="table">
        /// <item><term>200 OK</term><description>Import thành công, trả về ImportResultDto với thông tin chi tiết:
        ///   <list type="bullet">
        ///   <item><description><strong>TotalRows:</strong> Tổng số dòng đã xử lý</description></item>
        ///   <item><description><strong>SuccessfulRows:</strong> Số dòng import thành công</description></item>
        ///   <item><description><strong>FailedRows:</strong> Số dòng thất bại</description></item>
        ///   <item><description><strong>IsSuccess:</strong> true nếu có ít nhất 1 dòng thành công</description></item>
        ///   <item><description><strong>Errors:</strong> Danh sách lỗi chi tiết cho từng dòng thất bại</description></item>
        ///   </list>
        /// </description></item>
        /// <item><term>400 Bad Request</term><description>
        ///   <list type="bullet">
        ///   <item><description>File không được chọn hoặc rỗng</description></item>
        ///   <item><description>File không đúng định dạng .xlsx</description></item>
        ///   <item><description>ImportResultDto.IsSuccess = false (tất cả dòng thất bại)</description></item>
        ///   </list>
        /// </description></item>
        /// <item><term>500 Internal Server Error</term><description>Lỗi hệ thống trong quá trình xử lý file hoặc database</description></item>
        /// </list>
        /// </returns>
        /// <exception cref="ArgumentNullException">Ném khi file parameter là null</exception>
        /// <exception cref="InvalidDataException">Ném khi file Excel có cấu trúc không hợp lệ</exception>
        /// <exception cref="ValidationException">Ném khi dữ liệu trong file vi phạm validation rules</exception>
        /// <exception cref="IOException">Ném khi có lỗi đọc file</exception>
        [HttpPost("import-residents")]
        public async Task<IActionResult> ImportResidents(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Vui lòng chọn một file Excel để tải lên.");
            }
            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Chỉ hỗ trợ file Excel (.xlsx).");
            }

            try
            {
                await using var stream = file.OpenReadStream();
                var result = await _userService.ImportResidentsFromExcelAsync(stream);

                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }

    }
}
