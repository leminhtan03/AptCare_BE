using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.CommonAreaObjectDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class CommonAreaObjectController : BaseApiController
    {
        private readonly ICommonAreaObjectService _commonAreaObjectService;

        public CommonAreaObjectController(ICommonAreaObjectService commonAreaObjectService)
        {
            _commonAreaObjectService = commonAreaObjectService;
        }

        /// <summary>
        /// Lấy danh sách đối tượng khu vực chung có phân trang, tìm kiếm và sắp xếp.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.    
        ///  
        /// **Tham số phân trang (PaginateDto):**
        /// - <b>page</b>: Số trang hiện tại (bắt đầu từ 1).  
        /// - <b>size</b>: Số bản ghi mỗi trang.  
        /// - <b>search</b>: Từ khóa tìm kiếm (theo tên, mô tả).  
        /// - <b>filter</b>: Lọc theo trạng thái (Active/Inactive).  
        /// - <b>sortBy</b>: Tiêu chí sắp xếp kết quả:
        ///   - <b>name</b>: Sắp xếp theo tên tăng dần.  
        ///   - <b>name_desc</b>: Sắp xếp theo tên giảm dần.  
        ///   - <b>common_area</b>: Sắp xếp theo ID khu vực chung tăng dần.  
        ///   - <b>common_area_desc</b>: Sắp xếp theo ID khu vực chung giảm dần.  
        /// </remarks>
        /// <param name="dto">Thông tin phân trang, tìm kiếm, sắp xếp và lọc.</param>
        /// <param name="commonAreaId">ID khu vực chung (tùy chọn để lọc theo khu vực chung cụ thể).</param>
        /// <returns>Danh sách đối tượng khu vực chung kèm thông tin phân trang.</returns>
        /// <response code="200">Trả về danh sách đối tượng khu vực chung.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(IPaginate<CommonAreaObjectDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetPaginateCommonAreaObject([FromQuery] PaginateDto dto, int? commonAreaId)
        {
            var result = await _commonAreaObjectService.GetPaginateCommonAreaObjectAsync(dto, commonAreaId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách đối tượng theo ID khu vực chung.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.  
        ///  
        /// Dùng khi cần hiển thị các đối tượng thuộc một khu vực chung cụ thể.
        /// </remarks>
        /// <param name="commonAreaId">ID của khu vực chung cần lấy danh sách đối tượng.</param>
        /// <returns>Danh sách đối tượng thuộc khu vực chung đó.</returns>
        /// <response code="200">Trả về danh sách đối tượng theo khu vực chung.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet("by-common-area/{commonAreaId}")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<CommonAreaObjectBasicDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetCommonAreaObjectsByCommonArea(int commonAreaId)
        {
            var result = await _commonAreaObjectService.GetCommonAreaObjectsByCommonAreaAsync(commonAreaId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một đối tượng khu vực chung theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.  
        /// </remarks>
        /// <param name="id">ID của đối tượng khu vực chung cần lấy thông tin.</param>
        /// <returns>Thông tin chi tiết của đối tượng khu vực chung.</returns>
        /// <response code="200">Trả về thông tin đối tượng khu vực chung.</response>
        /// <response code="404">Không tìm thấy đối tượng khu vực chung.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(CommonAreaObjectDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetCommonAreaObjectById(int id)
        {
            var result = await _commonAreaObjectService.GetCommonAreaObjectByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Tạo mới một đối tượng khu vực chung.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager  
        ///  
        /// Dữ liệu yêu cầu gồm: tên đối tượng, mô tả, ID khu vực chung.
        /// Trạng thái sẽ tự động được đặt là Active khi tạo mới.
        /// </remarks>
        /// <param name="dto">Thông tin đối tượng khu vực chung cần tạo.</param>
        /// <returns>Thông báo tạo đối tượng khu vực chung thành công.</returns>
        /// <response code="201">Đối tượng khu vực chung được tạo thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="404">Không tìm thấy khu vực chung.</response>
        /// <response code="409">Tên đối tượng đã tồn tại trong khu vực chung này.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPost]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> CreateCommonAreaObject(CommonAreaObjectCreateDto dto)
        {
            var result = await _commonAreaObjectService.CreateCommonAreaObjectAsync(dto);
            return Created(string.Empty, result);
        }

        /// <summary>
        /// Cập nhật thông tin đối tượng khu vực chung theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager  
        ///  
        /// Cập nhật thông tin như: tên, mô tả, trạng thái, ID khu vực chung.
        /// </remarks>
        /// <param name="id">ID của đối tượng khu vực chung cần cập nhật.</param>
        /// <param name="dto">Thông tin đối tượng khu vực chung cập nhật.</param>
        /// <returns>Thông báo cập nhật thành công.</returns>
        /// <response code="200">Cập nhật đối tượng khu vực chung thành công.</response>
        /// <response code="404">Không tìm thấy đối tượng khu vực chung hoặc khu vực chung.</response>
        /// <response code="409">Tên đối tượng đã tồn tại trong khu vực chung này.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPut("{id}")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpdateCommonAreaObject(int id, CommonAreaObjectUpdateDto dto)
        {
            var result = await _commonAreaObjectService.UpdateCommonAreaObjectAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Xóa một đối tượng khu vực chung theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager  
        ///  
        /// Xóa đối tượng khu vực chung sẽ không ảnh hưởng đến các dữ liệu khác ngoài quan hệ trực tiếp (nếu có).
        /// </remarks>
        /// <param name="id">ID của đối tượng khu vực chung cần xóa.</param>
        /// <returns>Thông báo xóa thành công.</returns>
        /// <response code="200">Đối tượng khu vực chung được xóa thành công.</response>
        /// <response code="404">Không tìm thấy đối tượng khu vực chung.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeleteCommonAreaObject(int id)
        {
            var result = await _commonAreaObjectService.DeleteCommonAreaObjectAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Kích hoạt đối tượng khu vực chung.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager  
        ///  
        /// Chuyển trạng thái của đối tượng khu vực chung từ Inactive sang Active.
        /// Lưu ý: Không thể kích hoạt nếu khu vực chung cha đã bị vô hiệu hóa.
        /// </remarks>
        /// <param name="id">ID của đối tượng khu vực chung cần kích hoạt.</param>
        /// <returns>Thông báo kích hoạt thành công.</returns>
        /// <response code="200">Kích hoạt đối tượng khu vực chung thành công.</response>
        /// <response code="404">Không tìm thấy đối tượng khu vực chung.</response>
        /// <response code="400">Đối tượng khu vực chung đã ở trạng thái hoạt động hoặc khu vực chung cha đã bị vô hiệu hóa.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPatch("{id}/activate")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> ActivateCommonAreaObject(int id)
        {
            var result = await _commonAreaObjectService.ActivateCommonAreaObjectAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Vô hiệu hóa đối tượng khu vực chung.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager  
        ///  
        /// Chuyển trạng thái của đối tượng khu vực chung từ Active sang Inactive.
        /// </remarks>
        /// <param name="id">ID của đối tượng khu vực chung cần vô hiệu hóa.</param>
        /// <returns>Thông báo vô hiệu hóa thành công.</returns>
        /// <response code="200">Vô hiệu hóa đối tượng khu vực chung thành công.</response>
        /// <response code="404">Không tìm thấy đối tượng khu vực chung.</response>
        /// <response code="400">Đối tượng khu vực chung đã ở trạng thái ngưng hoạt động.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPatch("{id}/deactivate")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeactivateCommonAreaObject(int id)
        {
            var result = await _commonAreaObjectService.DeactivateCommonAreaObjectAsync(id);
            return Ok(result);
        }
    }
}