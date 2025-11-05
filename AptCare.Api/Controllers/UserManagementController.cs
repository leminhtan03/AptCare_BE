using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.Account;
using AptCare.Service.Dtos.UserDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AptCare.Api.Controllers
{
    [Authorize(Roles = nameof(AccountRole.Manager))]
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
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetUserById(int id)
        {
            var result = await _userService.GetUserByIdAsync(id);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }
        /// <summary>
        /// Cập nhật thông tin người dùng theo ID được chỉ định.
        /// </summary>
        /// <remarks>
        /// <para>Endpoint này cho phép cập nhật thông tin người dùng bao gồm dữ liệu cá nhân, phân công căn hộ và trạng thái.</para>
        /// <para>Chỉ những thuộc tính không null trong UpdateUserDto mới được cập nhật (partial update).</para>
        /// <para><strong>Lưu ý:</strong> Endpoint này yêu cầu quyền Manager .</para>
        /// </remarks>
        /// <param name="id">ID duy nhất của người dùng cần cập nhật (phải là số nguyên dương).</param>
        /// <param name="updateUserDto">Đối tượng chứa thông tin cần cập nhật.
        /// <para><strong>Các thuộc tính bao gồm:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>FirstName:</strong> Tên (tối đa 256 ký tự, có validation)</description></item>
        /// <item><description><strong>LastName:</strong> Họ (tối đa 256 ký tự, có validation)</description></item>
        /// <item><description><strong>CitizenshipIdentity:</strong> Số CCCD/CMND (tối đa 50 ký tự)</description></item>
        /// <item><description><strong>Birthday:</strong> Ngày sinh (định dạng DateTime)</description></item>
        /// <item><description><strong>PhoneNumber:</strong> Số điện thoại (tối đa 20 ký tự, có validation)</description></item>
        /// <item><description><strong>Status:</strong> Trạng thái người dùng</description></item>
        /// <item><description><strong>Email:</strong> Địa chỉ email (tối đa 256 ký tự, có validation)</description></item>
        /// <item><description><strong>AccountRole:</strong> Vai trò tài khoản (Enum: "Manager", "Resident", "Receptionist", "Technician","TechnicianLead")</description></item>
        /// <item><description><strong>UserApartments:</strong> Danh sách căn hộ được phân công: <br/>
        /// <strong>CHỈ Có Role RESIDENT mới có list này </strong>
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
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            var result = await _userService.UpdateUserAsync(id, updateUserDto);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }
        /// <summary>
        /// Lấy danh sách dữ liệu cư dân theo trang với khả năng lọc và tìm kiếm. Nó sẽ lấy tất cả thông tin của cư dân bao gồm dữ liệu cá nhân, căn hộ được phân công và trạng thái tài khoản. (Ko có các thông tin của nhân viên)
        /// </summary>
        /// <remarks>
        /// <para><strong>Lưu ý:</strong> Endpoint này yêu cầu quyền Manager.</para>
        /// 
        /// <para><strong>Ví dụ query parameters:</strong></para>
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
        [HttpGet("residents_data")]
        [ProducesResponseType(typeof(IPaginate<UserGetAllDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GetResidentDataPage([FromQuery] GetResidentDataFilterDto getResidentDataFilterDto)
        {
            var result = await _userService.GetReSidentDataPageAsync(getResidentDataFilterDto.SearchQuery, getResidentDataFilterDto.Status, getResidentDataFilterDto.Page, getResidentDataFilterDto.PageSize);
            return Ok(result);
        }

        /// <summary>
        /// Nhập dữ liệu cư dân từ file Excel với xác thực và xử lý lỗi chi tiết.
        /// </summary>
        /// <remarks>
        /// <para>Endpoint này cho phép nhập hàng loạt thông tin cư dân từ file Excel (.xlsx) với xác thực dữ liệu đầy đủ.</para>
        /// <para>Hệ thống sẽ validate từng dòng dữ liệu và cung cấp báo cáo chi tiết về các lỗi nếu có.</para>
        /// <para><strong>Lưu ý:</strong> Endpoint này yêu cầu quyền Manager.</para>
        /// 
        /// <para><strong>Định dạng file Excel yêu cầu:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Định dạng:</strong> .xlsx (Excel 2007 trở lên)</description></item>
        /// </list>
        /// </remarks>
        /// <returns>
        /// <para><strong>Các trường hợp trả về:</strong></para>
        /// <list type="table">
        /// <item><term>200 OK</term><description>Nhập thành công, trả về ImportResultDto với:
        ///   <list type="bullet">
        ///   <item><description><strong>TotalRows:</strong> Tổng số dòng đã xử lý</description></item>
        ///   <item><description><strong>SuccessfulRows:</strong> Số dòng nhập thành công</description></item>
        ///   <item><description><strong>FailedRows:</strong> Số dòng thất bại</description></item>
        ///   <item><description><strong>IsSuccess:</strong> Trạng thái tổng quát của quá trình nhập</description></item>
        ///   <item><description><strong>Errors:</strong> Danh sách lỗi chi tiết (nếu có)</description></item>
        ///   </list>
        /// </description></item>
        /// <item><term>400 Bad Request</term><description>File không hợp lệ (null, empty, hoặc không phải .xlsx)</description></item>
        /// <item><term>500 Internal Server Error</term><description>Lỗi hệ thống trong quá trình xử lý file</description></item>
        /// </list>
        /// </returns>
        /// <exception cref="ArgumentException">Ném khi file không hợp lệ hoặc định dạng sai</exception>
        /// <exception cref="InvalidDataException">Ném khi cấu trúc dữ liệu trong Excel không đúng</exception>
        /// <exception cref="IOException">Ném khi có lỗi đọc file</exception>
        [HttpPost("import-residents")]
        [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ImportResidentsorApartmentsInfo(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Vui lòng chọn một file Excel để tải lên.");
            }
            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Chỉ hỗ trợ file Excel (.xlsx).");
            }

            await using var stream = file.OpenReadStream();

            var result = await _userService.ImportResidentsFromExcelAsync(stream);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        /// <summary>
        /// Tạo mới một người dùng trong hệ thống với tùy chọn tạo account ngay.
        /// </summary>
        /// <remarks>
        /// <para>Endpoint này cho phép tạo mới người dùng với tùy chọn tạo account đăng nhập ngay:</para>
        /// 
        /// <para><strong>Quy tắc tạo Account:</strong></para>
        /// <list type="number">
        /// <item><description><strong>Resident:</strong> Tùy chọn tạo account (set CreateAccount = true/false)</description></item>
        /// <item><description><strong>Technician/TechnicianLead:</strong> Luôn tạo account tự động (ignore CreateAccount)</description></item>
        /// <item><description><strong>Manager/Receptionist/Admin:</strong> Luôn tạo account tự động (ignore CreateAccount)</description></item>
        /// </list>
        /// 
        /// <para><strong>Thông tin Account tự động:</strong></para>
        /// <list type="bullet">
        /// <item><description>Username = Email của user</description></item>
        /// <item><description>Password = Random password (12 ký tự, gửi qua email)</description></item>
        /// <item><description>MustChangePassword = true (bắt buộc đổi password lần đầu)</description></item>
        /// <item><description>EmailConfirmed = false (cần verify email)</description></item>
        /// </list>
        /// 
        /// <para><strong>Ví dụ request - Tạo Resident có account:</strong></para>
        /// <code>
        /// {
        ///   "firstName": "Nguyen",
        ///   "lastName": "Van A",
        ///   "phoneNumber": "0901234567",
        ///   "email": "nguyenvana@example.com",
        ///   "citizenshipIdentity": "001234567890",
        ///   "birthday": "1990-01-15",
        ///   "role": "Resident",
        ///   "createAccount": true,
        ///   "apartments": [
        ///     {
        ///       "apartmentId": 101,
        ///       "roleInApartment": "Owner",
        ///       "relationshipToOwner": "Self"
        ///     }
        ///   ]
        /// }
        /// </code>
        /// 
        /// <para><strong>Ví dụ request - Tạo Technician (tự động có account):</strong></para>
        /// <code>
        /// {
        ///   "firstName": "Tran",
        ///   "lastName": "Van B",
        ///   "phoneNumber": "0907654321",
        ///   "email": "tranvanb@example.com",
        ///   "citizenshipIdentity": "009876543210",
        ///   "role": "Technician",
        ///   "createAccount": false,  // ignored, vẫn tạo account
        ///   "techniqueIds": [1, 3, 5]
        /// }
        /// </code>
        /// </remarks>
        [HttpPost("create-user-data")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            var result = await _userService.CreateUserAsync(createUserDto);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật ảnh đại diện của người dùng theo ID được chỉ định.
        /// </summary>
        /// <remarks>
        /// <para>Endpoint này cho phép cập nhật ảnh đại diện cho người dùng bằng cách upload file ảnh mới.</para>
        /// <para>File ảnh sẽ được validate và lưu trữ trong hệ thống, sau đó URL ảnh mới sẽ được cập nhật vào thông tin người dùng.</para>
        /// <para><strong>Lưu ý:</strong> Endpoint này yêu cầu quyền Manager.</para>
        /// 
        /// <para><strong>Yêu cầu file ảnh:</strong></para>
        /// <list type="bullet">
        /// <item><description>Định dạng hỗ trợ: JPG, JPEG, PNG, GIF</description></item>
        /// <item><description>Kích thước tối đa: tùy theo cấu hình hệ thống</description></item>
        /// <item><description>File không được null hoặc rỗng</description></item>
        /// </list>
        /// </remarks>
        /// <param name="dto">Đối tượng chứa thông tin cập nhật ảnh đại diện.
        /// <para><strong>Các thuộc tính bao gồm:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>UserId:</strong> ID của người dùng cần cập nhật ảnh (bắt buộc)</description></item>
        /// <item><description><strong>ImageProfileUrl:</strong> File ảnh đại diện mới (IFormFile, bắt buộc)</description></item>
        /// </list>
        /// </param>
        /// <returns>
        /// <para><strong>Các trường hợp trả về:</strong></para>
        /// <list type="table">
        /// <item><term>200 OK</term><description>Cập nhật thành công, trả về thông báo "Cập nhật ảnh đại diện thành công."</description></item>
        /// <item><term>400 Bad Request</term><description>Dữ liệu đầu vào không hợp lệ (UserId không tồn tại, file ảnh không hợp lệ hoặc null)</description></item>
        /// <item><term>404 Not Found</term><description>Không tìm thấy người dùng với ID được chỉ định</description></item>
        /// <item><term>500 Internal Server Error</term><description>Lỗi hệ thống trong quá trình xử lý hoặc lưu trữ file</description></item>
        /// </list>
        /// </returns>
        /// <exception cref="ArgumentException">Ném khi UserId không hợp lệ hoặc file ảnh không đúng định dạng</exception>
        /// <exception cref="InvalidOperationException">Ném khi không thể lưu trữ file ảnh</exception>
        /// <exception cref="IOException">Ném khi có lỗi đọc hoặc ghi file</exception>
        [HttpPut("update-user-profile-image")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateUserProfileImage([FromBody] UpdateUserImageProfileDto dto)
        {
            await _userService.UpdateUserProfileImageAsync(dto);
            return Ok("Cập nhật ảnh đại diện thành công.");
        }

    }
}
