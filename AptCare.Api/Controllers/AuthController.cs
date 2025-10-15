using AptCare.Repository.Paginate;
using AptCare.Service.Dtos.Account;
using AptCare.Service.Dtos.AuthenDto;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : BaseApiController
    {
        private readonly IAuthenticationService _authenService;

        public AuthController(IAuthenticationService registration) => _authenService = registration;

        /// <summary>
        /// Đăng ký tài khoản mới cho người dùng.
        /// </summary>
        /// <remarks>
        /// **Chức năng:** Tạo tài khoản mới và gửi OTP xác thực email.
        ///  
        /// **Thông tin đăng ký (RegisterRequestDto):**
        /// - <b>Email</b>: Địa chỉ email hợp lệ (bắt buộc).  
        /// - <b>Password</b>: Mật khẩu tối thiểu 6 ký tự (bắt buộc).  
        /// **Lưu ý:**
        /// - Email phải chưa được sử dụng trong hệ thống.
        /// - Sau khi đăng ký thành công, OTP sẽ được gửi đến email để xác thực.
        /// </remarks>
        /// <param name="dto">Thông tin đăng ký bao gồm email và mật khẩu.</param>
        /// <returns>Thông tin tài khoản đã tạo và trạng thái gửi OTP.</returns>
        /// <response code="201">Đăng ký thành công, OTP đã được gửi.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ hoặc email đã tồn tại.</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            var res = await _authenService.RegisterAsync(dto);
            return Created("", res);
        }
        /// <summary>
        /// Gửi lại mã OTP xác thực email cho tài khoản.
        /// </summary>
        /// <remarks>
        /// **Chức năng:** Gửi lại mã OTP xác thực email khi người dùng không nhận được hoặc mã đã hết hạn.
        ///  
        /// **Lưu ý:**
        /// - Tài khoản phải tồn tại và chưa được xác thực email.
        /// - Chỉ có thể gửi lại OTP sau một khoảng thời gian nhất định.
        /// </remarks>
        /// <param name="accountId">ID của tài khoản cần gửi lại OTP xác thực email.</param>
        /// <returns>Không có nội dung trả về khi gửi thành công.</returns>
        /// <response code="204">Gửi lại OTP thành công.</response>
        /// <response code="400">Tài khoản không tồn tại hoặc không thể gửi lại OTP.</response>
        [HttpPost("register/resend-otp")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResendOtp([FromQuery] int accountId)
        {
            await _authenService.ResendEmailVerificationOtpAsync(accountId);
            return NoContent();
        }
        /// <summary>
        /// Xác nhận đặt lại mật khẩu với token và mật khẩu mới.
        /// </summary>
        /// <remarks>
        /// **Chức năng:** Hoàn tất quá trình đặt lại mật khẩu bằng cách sử dụng token xác thực và mật khẩu mới.
        ///  
        /// **Thông tin đặt lại mật khẩu (PasswordResetConfirmDto):**
        /// - <b>AccountId</b>: ID của tài khoản cần đặt lại mật khẩu (bắt buộc).
        /// - <b>ResetToken</b>: Token xác thực được cấp sau khi verify OTP thành công (bắt buộc).
        /// - <b>NewPassword</b>: Mật khẩu mới tối thiểu 6 ký tự (bắt buộc).
        ///  
        /// **Lưu ý:**
        /// - Token phải hợp lệ và chưa hết hạn.
        /// - Mật khẩu mới sẽ thay thế mật khẩu cũ sau khi xác nhận thành công.
        /// </remarks>
        /// <param name="dto">Thông tin xác nhận đặt lại mật khẩu bao gồm AccountId, ResetToken và mật khẩu mới.</param>
        /// <returns>Không có nội dung trả về khi đặt lại mật khẩu thành công.</returns>
        /// <response code="204">Đặt lại mật khẩu thành công.</response>
        /// <response code="400">Token không hợp lệ hoặc đã hết hạn, hoặc dữ liệu đầu vào không hợp lệ.</response>

        [HttpPost("register/verify")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Verify([FromBody] VerifyDto dto)
        {
            var ok = await _authenService.VerifyEmailAsync(dto.AccountId, dto.Otp);
            return ok ? NoContent() : BadRequest("OTP không hợp lệ hoặc đã hết hạn.");
        }
        /// <summary>
        /// Xác thực email và đăng nhập tự động cho người dùng.
        /// </summary>
        /// <remarks>
        /// **Chức năng:** Xác thực mã OTP email và tự động đăng nhập người dùng vào hệ thống sau khi xác thực thành công.
        ///  
        /// **Thông tin xác thực và đăng nhập (VerifyAndLoginDto):**
        /// - <b>AccountId</b>: ID của tài khoản cần xác thực (bắt buộc).
        /// - <b>Otp</b>: Mã OTP được gửi qua email (bắt buộc).
        /// - <b>DeviceId</b>: ID thiết bị để quản lý phiên đăng nhập (bắt buộc).
        ///  
        /// **Lưu ý:**
        /// - Mã OTP phải hợp lệ và chưa hết hạn.
        /// - Sau khi xác thực thành công, người dùng sẽ được đăng nhập tự động.
        /// - Trả về access token và refresh token để sử dụng cho các API yêu cầu xác thực.
        /// </remarks>
        /// <param name="dto">Thông tin xác thực bao gồm AccountId, OTP và DeviceId.</param>
        /// <returns>Cặp token (AccessToken và RefreshToken) để xác thực các request tiếp theo.</returns>
        /// <response code="200">Xác thực và đăng nhập thành công, trả về token.</response>
        /// <response code="400">Mã OTP không hợp lệ hoặc đã hết hạn, hoặc dữ liệu đầu vào không hợp lệ.</response>
        [HttpPost("register/verify-and-login")]
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyAndLogin([FromBody] VerifyAndLoginDto dto)
        {
            var tokens = await _authenService.VerifyEmailAndLoginAsync(dto.AccountId, dto.Otp, dto.DeviceId);
            return Ok(tokens);
        }
        /// <summary>
        /// Đăng nhập vào hệ thống với tài khoản người dùng.
        /// </summary>
        /// <remarks>
        /// **Chức năng:** Xác thực người dùng và cấp token truy cập hệ thống.
        ///  
        /// **Thông tin đăng nhập (LoginRequestDto):**
        /// - <b>UsernameOrEmail</b>: Tên đăng nhập hoặc địa chỉ email (bắt buộc).
        /// - <b>Password</b>: Mật khẩu tài khoản (bắt buộc).
        /// - <b>DeviceId</b>: ID thiết bị đăng nhập để quản lý phiên (bắt buộc).
        ///  
        /// **Lưu ý:**
        /// - Tài khoản phải tồn tại và đã được kích hoạt.
        /// - Đối với tài khoản đăng nhập lần đầu, hệ thống sẽ yêu cầu đổi mật khẩu mặc định.
        /// - Trả về access token và refresh token để sử dụng cho các API yêu cầu xác thực.
        /// 
        /// **Xử lý PasswordChangeRequiredException:**
        /// - Exception này được ném ra khi người dùng đăng nhập lần đầu hoặc tài khoản bị yêu cầu thay đổi mật khẩu.
        /// - API sẽ trả về mã 403 (Forbidden) với code "PASSWORD_CHANGE_REQUIRED" và accountId.
        /// - Client cần chuyển hướng người dùng đến trang đổi mật khẩu và gọi API "password/first-change" để đặt mật khẩu mới.
        /// - Chỉ sau khi đổi mật khẩu thành công, người dùng mới có thể đăng nhập vào hệ thống.
        /// </remarks>
        /// <param name="dto">Thông tin đăng nhập bao gồm UsernameOrEmail, Password và DeviceId.</param>
        /// <returns>Cặp token (AccessToken và RefreshToken) để xác thực các request tiếp theo.</returns>
        /// <response code="200">Đăng nhập thành công, trả về token.</response>
        /// <response code="400">Thông tin đăng nhập không chính xác hoặc tài khoản chưa được xác thực.</response>
        /// <response code="403">Yêu cầu thay đổi mật khẩu trước khi đăng nhập.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            try
            {
                var tokens = await _authenService.LoginAsync(dto);
                return Ok(tokens);
            }
            catch (PasswordChangeRequiredException ex)
            {
                // 403 Forbidden với code riêng và AccountId để FE biết tài khoản nào
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    code = "PASSWORD_CHANGE_REQUIRED",
                    accountId = ex.AccountId,
                    message = "Bạn cần đổi mật khẩu trước khi đăng nhập."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        /// <summary>
        /// Yêu cầu đặt lại mật khẩu cho tài khoản người dùng.
        /// </summary>
        /// <remarks>
        /// **Chức năng:** Khởi tạo quá trình đặt lại mật khẩu bằng cách gửi mã OTP xác thực đến email của người dùng.
        ///  
        /// **Thông tin yêu cầu (PasswordResetRequestDto):**
        /// - <b>Email</b>: Địa chỉ email của tài khoản cần đặt lại mật khẩu (bắt buộc).
        ///  
        /// **Lưu ý:**
        /// - Email phải tồn tại trong hệ thống và đã được xác thực.
        /// - Mã OTP sẽ được gửi đến email để xác thực danh tính trước khi đặt lại mật khẩu.
        /// - Mã OTP có thời gian hết hạn nhất định.
        /// </remarks>
        /// <param name="dto">Thông tin yêu cầu đặt lại mật khẩu bao gồm email tài khoản.</param>
        /// <returns>Không có nội dung trả về khi gửi OTP thành công.</returns>
        /// <response code="200">Gửi yêu cầu đặt lại mật khẩu thành công, OTP đã được gửi.</response>
        /// <response code="400">Email không tồn tại hoặc không hợp lệ.</response>
        [HttpPost("password-reset/request")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PasswordResetRequest([FromBody] PasswordResetRequestDto dto)
        {
            await _authenService.PasswordResetRequestAsync(dto);
            return Ok();
        }
        /// <summary>
        /// Xác thực mã OTP để đặt lại mật khẩu và nhận token xác nhận.
        /// </summary>
        /// <remarks>
        /// **Chức năng:** Xác thực mã OTP được gửi qua email trong quá trình đặt lại mật khẩu và trả về token để xác nhận đặt lại mật khẩu.
        ///  
        /// **Thông tin xác thực OTP (PasswordResetVerifyOtpDto):**
        /// - <b>AccountId</b>: ID của tài khoản cần đặt lại mật khẩu (bắt buộc).
        /// - <b>Otp</b>: Mã OTP được gửi qua email (bắt buộc).
        ///  
        /// **Lưu ý:**
        /// - Mã OTP phải hợp lệ và chưa hết hạn.
        /// - Token trả về sẽ được sử dụng để xác nhận đặt lại mật khẩu ở bước tiếp theo.
        /// - Token có thời gian hết hạn nhất định.
        /// </remarks>
        /// <param name="dto">Thông tin xác thực OTP bao gồm AccountId và mã OTP.</param>
        /// <returns>Token xác nhận để sử dụng cho việc đặt lại mật khẩu.</returns>
        /// <response code="200">Xác thực OTP thành công, trả về reset token.</response>
        /// <response code="400">Mã OTP không hợp lệ hoặc đã hết hạn, hoặc dữ liệu đầu vào không hợp lệ.</response>
        [HttpPost("password-reset/verify-otp")]
        [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PasswordResetVerifyOtp([FromBody] PasswordResetVerifyOtpDto dto)
        {
            var resetToken = await _authenService.PasswordResetVerifyOtpAsync(dto);
            return Ok(new { resetToken });
        }
        /// <summary>
        /// Xác nhận và hoàn tất quá trình đặt lại mật khẩu.
        /// </summary>
        /// <remarks>
        /// **Chức năng:** Xác nhận đặt lại mật khẩu bằng cách sử dụng token xác nhận và mật khẩu mới.
        ///  
        /// **Thông tin xác nhận đặt lại mật khẩu (PasswordResetConfirmDto):**
        /// - <b>AccountId</b>: ID của tài khoản cần đặt lại mật khẩu (bắt buộc).
        /// - <b>ResetToken</b>: Token xác nhận được cấp sau khi xác thực mã OTP thành công (bắt buộc).
        /// - <b>NewPassword</b>: Mật khẩu mới tối thiểu 6 ký tự (bắt buộc).
        ///  
        /// **Lưu ý:**
        /// - Token phải hợp lệ và chưa hết hạn.
        /// - Mật khẩu mới sẽ thay thế mật khẩu cũ sau khi xác nhận thành công.
        /// </remarks>
        /// <param name="dto">Thông tin xác nhận đặt lại mật khẩu bao gồm AccountId, ResetToken và mật khẩu mới.</param>
        /// <returns>Không có nội dung trả về khi đặt lại mật khẩu thành công.</returns>
        /// <response code="204">Đặt lại mật khẩu thành công.</response>
        /// <response code="400">Token không hợp lệ hoặc đã hết hạn, hoặc dữ liệu đầu vào không hợp lệ.</response>
        [HttpPost("password-reset/confirm")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PasswordResetConfirm([FromBody] PasswordResetConfirmDto dto)
        {
            await _authenService.PasswordResetConfirmAsync(dto);
            return NoContent();
        }
        /// <summary>
        /// Lấy thông tin hồ sơ của người dùng đã đăng nhập.
        /// </summary>
        /// <remarks>
        /// **Chức năng:** Truy xuất thông tin hồ sơ đầy đủ của người dùng hiện đang đăng nhập.
        ///  
        /// **Lưu ý:**
        /// - API này yêu cầu người dùng đã được xác thực (đăng nhập).
        /// - Thông tin trả về bao gồm các thông tin cá nhân, danh sách căn hộ, và vai trò của người dùng.
        /// - Access token phải được đính kèm trong header của request.
        /// </remarks>
        /// <returns>Thông tin chi tiết hồ sơ của người dùng đang đăng nhập.</returns>
        /// <response code="200">Trả về thông tin hồ sơ người dùng thành công.</response>
        /// <response code="401">Người dùng chưa đăng nhập hoặc token không hợp lệ.</response>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(GetOwnProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetOwnProfile()
        {
            var profile = await _authenService.GetOwnProfile();
            return Ok(profile);
        }
        /// <summary>
        /// Đổi mật khẩu cho người dùng đăng nhập lần đầu.
        /// </summary>
        /// <remarks>
        /// **Chức năng:** Cho phép người dùng đổi mật khẩu mặc định khi đăng nhập lần đầu vào hệ thống.
        ///  
        /// **Thông tin đổi mật khẩu (FirstLoginChangePasswordDto):**
        /// - <b>AccountId</b>: ID của tài khoản cần đổi mật khẩu (bắt buộc).
        /// - <b>CurrentPassword</b>: Mật khẩu hiện tại/mặc định của tài khoản (bắt buộc).
        /// - <b>NewPassword</b>: Mật khẩu mới tối thiểu 6 ký tự (bắt buộc).
        /// - <b>DeviceInfo</b>: Thông tin thiết bị đăng nhập (bắt buộc).
        ///  
        /// **Lưu ý:**
        /// - Người dùng phải xác thực thành công với mật khẩu hiện tại.
        /// - Sau khi đổi mật khẩu thành công, người dùng được cấp token đăng nhập mới.
        /// - Mật khẩu mới không được trùng với mật khẩu cũ.
        /// </remarks>
        /// <param name="dto">Thông tin đổi mật khẩu bao gồm AccountId, mật khẩu hiện tại, mật khẩu mới và thông tin thiết bị.</param>
        /// <returns>Cặp token (AccessToken và RefreshToken) để xác thực các request tiếp theo.</returns>
        /// <response code="200">Đổi mật khẩu thành công, trả về token đăng nhập mới.</response>
        /// <response code="400">Mật khẩu hiện tại không chính xác hoặc mật khẩu mới không đáp ứng yêu cầu.</response>
        [HttpPost("password/first-change")]
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> FirstLoginChangePassword([FromBody] FirstLoginChangePasswordDto dto)
        {
            var tokens = await _authenService.FirstLoginChangePasswordAsync(dto);
            return Ok(tokens);
        }
    }
}
