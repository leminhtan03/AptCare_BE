using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos.RepairRequestDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class RepairRequestController : BaseApiController
    {
        private readonly IRepairRequestService _repairRequestService;

        public RepairRequestController(IRepairRequestService repairRequestService)
        {
            _repairRequestService = repairRequestService;
        }

        /// <summary>
        /// Tạo yêu cầu sửa chữa thông thường.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** cư dân hoặc lễ tân.  
        /// Hệ thống sẽ kiểm tra căn hộ, kỹ thuật viên phù hợp và tạo cuộc hẹn tương ứng.  
        /// Nếu có tệp đính kèm, file sẽ được tải lên Cloudinary.
        /// </remarks>
        /// <param name="dto">Thông tin yêu cầu sửa chữa.</param>
        /// <returns>Thông báo tạo yêu cầu thành công.</returns>
        /// <response code="201">Tạo yêu cầu sửa chữa thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        /// <response code="404">Không tìm thấy căn hộ hoặc vấn đề liên quan.</response>
        /// <response code="500">Lỗi hệ thống.</response>
        [HttpPost("normal")]
        [Authorize(Roles = $"{nameof(AccountRole.Resident)}, {nameof(AccountRole.Receptionist)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CreateNormalRepairRequest([FromForm] RepairRequestNormalCreateDto dto)
        {
            var result = await _repairRequestService.CreateNormalRepairRequestAsync(dto);
            return Created(string.Empty, result);
        }

        [HttpPost("emergency")]
        [Authorize(Roles = $"{nameof(AccountRole.Resident)}, {nameof(AccountRole.Receptionist)}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CreateEmergencyRepairRequest([FromForm] RepairRequestEmergencyCreateDto dto)
        {
            var result = await _repairRequestService.CreateEmergencyRepairRequestAsync(dto);
            return Created(string.Empty, result);
        }

    }
}
