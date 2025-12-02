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
        /// <b>Chỉ role:</b> tất cả người dùng đã đăng nhập.<br/>
        /// <b>Tham số phân trang (<c>PaginateDto</c>):</b>
        /// <ul>
        ///   <li><b>page</b>: Số trang hiện tại (bắt đầu từ 1).</li>
        ///   <li><b>size</b>: Số bản ghi mỗi trang.</li>
        ///   <li><b>search</b>: Từ khóa tìm kiếm (theo tên, mô tả).</li>
        ///   <li><b>filter</b>: Lọc theo trạng thái (Active/Inactive).</li>
        ///   <li><b>sortBy</b>: Tiêu chí sắp xếp (name, name_desc, common_area, common_area_desc).</li>
        /// </ul>
        /// <b>commonAreaId</b>: ID khu vực chung (tùy chọn để lọc theo khu vực chung cụ thể).
        /// </remarks>
        /// <param name="dto">
        /// <b>PaginateDto:</b>
        /// <ul>
        ///   <li><b>page</b>: Số trang hiện tại.</li>
        ///   <li><b>size</b>: Số bản ghi mỗi trang.</li>
        ///   <li><b>search</b>: Từ khóa tìm kiếm.</li>
        ///   <li><b>filter</b>: Lọc theo trạng thái.</li>
        ///   <li><b>sortBy</b>: Tiêu chí sắp xếp.</li>
        /// </ul>
        /// </param>
        /// <param name="commonAreaId">ID khu vực chung (tùy chọn để lọc).</param>
        /// <returns>
        /// <b>IPaginate&lt;CommonAreaObjectDto&gt;:</b>
        /// <ul>
        ///   <li><b>CommonAreaObjectId</b>: ID đối tượng khu vực chung.</li>
        ///   <li><b>Name</b>: Tên đối tượng.</li>
        ///   <li><b>Description</b>: Mô tả đối tượng.</li>
        ///   <li><b>Status</b>: Trạng thái (Active/Inactive).</li>
        ///   <li><b>CommonArea</b>: Thông tin khu vực chung liên kết.</li>
        /// </ul>
        /// </returns>
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
        /// <b>Chỉ role:</b> tất cả người dùng đã đăng nhập.<br/>
        /// Dùng khi cần hiển thị các đối tượng thuộc một khu vực chung cụ thể.
        /// </remarks>
        /// <param name="commonAreaId">ID của khu vực chung cần lấy danh sách đối tượng.</param>
        /// <returns>
        /// <b>CommonAreaObjectBasicDto[]:</b>
        /// <ul>
        ///   <li><b>CommonAreaObjectId</b>: ID đối tượng khu vực chung.</li>
        ///   <li><b>Name</b>: Tên đối tượng.</li>
        ///   <li><b>Status</b>: Trạng thái đối tượng.</li>
        /// </ul>
        /// </returns>
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
        /// <b>Chỉ role:</b> tất cả người dùng đã đăng nhập.
        /// </remarks>
        /// <param name="id">ID của đối tượng khu vực chung cần lấy thông tin.</param>
        /// <returns>
        /// <b>CommonAreaObjectDto:</b>
        /// <ul>
        ///   <li><b>CommonAreaObjectId</b>: ID đối tượng khu vực chung.</li>
        ///   <li><b>Name</b>: Tên đối tượng.</li>
        ///   <li><b>Description</b>: Mô tả đối tượng.</li>
        ///   <li><b>Status</b>: Trạng thái (Active/Inactive).</li>
        ///   <li><b>CommonArea</b>: Thông tin khu vực chung liên kết.</li>
        /// </ul>
        /// </returns>
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
        /// <b>Chỉ role:</b> Manager<br/>
        /// <b>Dữ liệu yêu cầu (<c>CommonAreaObjectCreateDto</c>):</b>
        /// <ul>
        ///   <li><b>CommonAreaId</b>: ID khu vực chung liên kết (bắt buộc).</li>
        ///   <li><b>Name</b>: Tên đối tượng (bắt buộc, tối đa 256 ký tự).</li>
        ///   <li><b>Description</b>: Mô tả đối tượng (tối đa 1000 ký tự, tùy chọn).</li>
        /// </ul>
        /// Trạng thái sẽ tự động được đặt là Active khi tạo mới.
        /// </remarks>
        /// <param name="dto">
        /// <b>CommonAreaObjectCreateDto:</b>
        /// <ul>
        ///   <li><b>CommonAreaId</b>: ID khu vực chung liên kết.</li>
        ///   <li><b>Name</b>: Tên đối tượng.</li>
        ///   <li><b>Description</b>: Mô tả đối tượng.</li>
        /// </ul>
        /// </param>
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
        /// <b>Chỉ role:</b> Manager<br/>
        /// <b>Dữ liệu cập nhật (<c>CommonAreaObjectUpdateDto</c>):</b>
        /// <ul>
        ///   <li><b>CommonAreaId</b>: ID khu vực chung liên kết (bắt buộc).</li>
        ///   <li><b>Name</b>: Tên đối tượng (bắt buộc, tối đa 256 ký tự).</li>
        ///   <li><b>Description</b>: Mô tả đối tượng (tối đa 1000 ký tự, tùy chọn).</li>
        ///   <li><b>Status</b>: Trạng thái đối tượng (Active/Inactive).</li>
        /// </ul>
        /// </remarks>
        /// <param name="id">ID của đối tượng khu vực chung cần cập nhật.</param>
        /// <param name="dto">
        /// <b>CommonAreaObjectUpdateDto:</b>
        /// <ul>
        ///   <li><b>CommonAreaId</b>: ID khu vực chung liên kết.</li>
        ///   <li><b>Name</b>: Tên đối tượng.</li>
        ///   <li><b>Description</b>: Mô tả đối tượng.</li>
        ///   <li><b>Status</b>: Trạng thái đối tượng.</li>
        /// </ul>
        /// </param>
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
        /// <b>Chỉ role:</b> Manager<br/>
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
        /// <b>Chỉ role:</b> Manager<br/>
        /// Chuyển trạng thái của đối tượng khu vực chung từ Inactive sang Active.<br/>
        /// <b>Lưu ý:</b> Không thể kích hoạt nếu khu vực chung cha đã bị vô hiệu hóa.
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
        /// <b>Chỉ role:</b> Manager<br/>
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