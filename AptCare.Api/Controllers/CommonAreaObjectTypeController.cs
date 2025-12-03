using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.CommonAreaObjectTypeDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class CommonAreaObjectTypeController : BaseApiController
    {
        private readonly ICommonAreaObjectTypeService _commonAreaObjectTypeService;

        public CommonAreaObjectTypeController(ICommonAreaObjectTypeService commonAreaObjectTypeService)
        {
            _commonAreaObjectTypeService = commonAreaObjectTypeService;
        }

        /// <summary>
        /// Lấy danh sách loại đối tượng khu vực chung có phân trang, tìm kiếm và sắp xếp.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> tất cả người dùng đã đăng nhập.<br/>
        /// <b>Tham số phân trang (<c>PaginateDto</c>):</b>
        /// <ul>
        ///   <li><b>page</b>: Số trang hiện tại (bắt đầu từ 1).</li>
        ///   <li><b>size</b>: Số bản ghi mỗi trang.</li>
        ///   <li><b>search</b>: Từ khóa tìm kiếm (theo tên loại, mô tả).</li>
        ///   <li><b>filter</b>: Lọc theo trạng thái (Active/Inactive).</li>
        ///   <li><b>sortBy</b>: Tiêu chí sắp xếp (name, name_desc, status, status_desc).</li>
        /// </ul>
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
        /// <returns>
        /// <b>IPaginate&lt;CommonAreaObjectTypeDto&gt;:</b>
        /// <ul>
        ///   <li><b>CommonAreaObjectTypeId</b>: ID loại đối tượng.</li>
        ///   <li><b>TypeName</b>: Tên loại đối tượng.</li>
        ///   <li><b>Description</b>: Mô tả loại đối tượng.</li>
        ///   <li><b>Status</b>: Trạng thái (Active/Inactive).</li>
        /// </ul>
        /// </returns>
        /// <response code="200">Trả về danh sách loại đối tượng khu vực chung.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(IPaginate<CommonAreaObjectTypeDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetPaginateCommonAreaObjectType([FromQuery] PaginateDto dto)
        {
            var result = await _commonAreaObjectTypeService.GetPaginateCommonAreaObjectTypeAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách tất cả loại đối tượng khu vực chung đang hoạt động.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> tất cả người dùng đã đăng nhập.<br/>
        /// Dùng khi cần dropdown hoặc select list các loại đối tượng.
        /// </remarks>
        /// <returns>
        /// <b>CommonAreaObjectTypeDto[]:</b>
        /// <ul>
        ///   <li><b>CommonAreaObjectTypeId</b>: ID loại đối tượng.</li>
        ///   <li><b>TypeName</b>: Tên loại đối tượng.</li>
        ///   <li><b>Description</b>: Mô tả loại đối tượng.</li>
        ///   <li><b>Status</b>: Trạng thái (Active).</li>
        /// </ul>
        /// </returns>
        /// <response code="200">Trả về danh sách loại đối tượng đang hoạt động.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet("list")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<CommonAreaObjectTypeDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetCommonAreaObjectTypes()
        {
            var result = await _commonAreaObjectTypeService.GetCommonAreaObjectTypesAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của loại đối tượng khu vực chung theo ID.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> tất cả người dùng đã đăng nhập.
        /// </remarks>
        /// <param name="id">ID của loại đối tượng cần lấy thông tin.</param>
        /// <returns>
        /// <b>CommonAreaObjectTypeDto:</b>
        /// <ul>
        ///   <li><b>CommonAreaObjectTypeId</b>: ID loại đối tượng.</li>
        ///   <li><b>TypeName</b>: Tên loại đối tượng.</li>
        ///   <li><b>Description</b>: Mô tả loại đối tượng.</li>
        ///   <li><b>Status</b>: Trạng thái (Active/Inactive).</li>
        /// </ul>
        /// </returns>
        /// <response code="200">Trả về thông tin loại đối tượng khu vực chung.</response>
        /// <response code="404">Không tìm thấy loại đối tượng khu vực chung.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(CommonAreaObjectTypeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetCommonAreaObjectTypeById(int id)
        {
            var result = await _commonAreaObjectTypeService.GetCommonAreaObjectTypeByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Tạo mới loại đối tượng khu vực chung.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Manager<br/>
        /// <b>Dữ liệu yêu cầu (<c>CommonAreaObjectTypeCreateDto</c>):</b>
        /// <ul>
        ///   <li><b>TypeName</b>: Tên loại đối tượng (bắt buộc, tối đa 100 ký tự, không trùng lặp).</li>
        ///   <li><b>Description</b>: Mô tả loại đối tượng (tối đa 500 ký tự, tùy chọn).</li>
        /// </ul>
        /// Trạng thái sẽ tự động được đặt là Active khi tạo mới.
        /// </remarks>
        /// <param name="dto">
        /// <b>CommonAreaObjectTypeCreateDto:</b>
        /// <ul>
        ///   <li><b>TypeName</b>: Tên loại đối tượng.</li>
        ///   <li><b>Description</b>: Mô tả loại đối tượng.</li>
        /// </ul>
        /// </param>
        /// <returns>Thông báo tạo loại đối tượng khu vực chung thành công.</returns>
        /// <response code="201">Loại đối tượng khu vực chung được tạo thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="409">Tên loại đối tượng đã tồn tại.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPost]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> CreateCommonAreaObjectType(CommonAreaObjectTypeCreateDto dto)
        {
            var result = await _commonAreaObjectTypeService.CreateCommonAreaObjectTypeAsync(dto);
            return Created(string.Empty, result);
        }

        /// <summary>
        /// Cập nhật thông tin loại đối tượng khu vực chung theo ID.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Manager<br/>
        /// <b>Dữ liệu cập nhật (<c>CommonAreaObjectTypeUpdateDto</c>):</b>
        /// <ul>
        ///   <li><b>TypeName</b>: Tên loại đối tượng (bắt buộc, tối đa 100 ký tự).</li>
        ///   <li><b>Description</b>: Mô tả loại đối tượng (tối đa 500 ký tự, tùy chọn).</li>
        ///   <li><b>Status</b>: Trạng thái loại đối tượng (Active/Inactive).</li>
        /// </ul>
        /// </remarks>
        /// <param name="id">ID của loại đối tượng cần cập nhật.</param>
        /// <param name="dto">
        /// <b>CommonAreaObjectTypeUpdateDto:</b>
        /// <ul>
        ///   <li><b>TypeName</b>: Tên loại đối tượng.</li>
        ///   <li><b>Description</b>: Mô tả loại đối tượng.</li>
        ///   <li><b>Status</b>: Trạng thái loại đối tượng.</li>
        /// </ul>
        /// </param>
        /// <returns>Thông báo cập nhật thành công.</returns>
        /// <response code="200">Cập nhật loại đối tượng khu vực chung thành công.</response>
        /// <response code="404">Không tìm thấy loại đối tượng khu vực chung.</response>
        /// <response code="409">Tên loại đối tượng đã tồn tại.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPut("{id}")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpdateCommonAreaObjectType(int id, CommonAreaObjectTypeUpdateDto dto)
        {
            var result = await _commonAreaObjectTypeService.UpdateCommonAreaObjectTypeAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Xóa loại đối tượng khu vực chung theo ID.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Manager<br/>
        /// <b>Lưu ý:</b>
        /// <ul>
        ///   <li>Không thể xóa nếu loại đối tượng đang có đối tượng liên kết.</li>
        ///   <li>Không thể xóa nếu loại đối tượng đang có nhiệm vụ bảo trì liên kết.</li>
        /// </ul>
        /// </remarks>
        /// <param name="id">ID của loại đối tượng cần xóa.</param>
        /// <returns>Thông báo xóa thành công.</returns>
        /// <response code="200">Loại đối tượng khu vực chung được xóa thành công.</response>
        /// <response code="404">Không tìm thấy loại đối tượng khu vực chung.</response>
        /// <response code="400">Không thể xóa do có dữ liệu liên kết.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeleteCommonAreaObjectType(int id)
        {
            var result = await _commonAreaObjectTypeService.DeleteCommonAreaObjectTypeAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Kích hoạt loại đối tượng khu vực chung.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Manager<br/>
        /// Chuyển trạng thái của loại đối tượng từ Inactive sang Active.
        /// </remarks>
        /// <param name="id">ID của loại đối tượng cần kích hoạt.</param>
        /// <returns>Thông báo kích hoạt thành công.</returns>
        /// <response code="200">Kích hoạt loại đối tượng khu vực chung thành công.</response>
        /// <response code="404">Không tìm thấy loại đối tượng khu vực chung.</response>
        /// <response code="400">Loại đối tượng đã ở trạng thái hoạt động.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPatch("{id}/activate")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> ActivateCommonAreaObjectType(int id)
        {
            var result = await _commonAreaObjectTypeService.ActivateCommonAreaObjectTypeAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Vô hiệu hóa loại đối tượng khu vực chung.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Manager<br/>
        /// Chuyển trạng thái của loại đối tượng từ Active sang Inactive.
        /// </remarks>
        /// <param name="id">ID của loại đối tượng cần vô hiệu hóa.</param>
        /// <returns>Thông báo vô hiệu hóa thành công.</returns>
        /// <response code="200">Vô hiệu hóa loại đối tượng khu vực chung thành công.</response>
        /// <response code="404">Không tìm thấy loại đối tượng khu vực chung.</response>
        /// <response code="400">Loại đối tượng đã ở trạng thái ngưng hoạt động.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPatch("{id}/deactivate")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeactivateCommonAreaObjectType(int id)
        {
            var result = await _commonAreaObjectTypeService.DeactivateCommonAreaObjectTypeAsync(id);
            return Ok(result);
        }
    }
}
