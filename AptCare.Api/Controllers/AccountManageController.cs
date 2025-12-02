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
        /// Tạo tài khoản cho user đã tồn tại (chủ yếu dành cho Resident chưa có account).
        /// </summary>
        /// <remarks>
        /// <para><strong>LƯU Ý QUAN TRỌNG:</strong></para>
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
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> CreateAccountForUser(int userid)
        {
            var result = await _accountService.CreateAccountForUserAsync(userid);
            return Ok(result);
        }
    }
}
