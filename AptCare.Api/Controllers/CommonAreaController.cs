using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sprache;

namespace AptCare.Api.Controllers
{
    public class CommonAreaController : BaseApiController
    {
        private readonly ICommonAreaService _commonAreaService;

        public CommonAreaController(ICommonAreaService commonAreaService)
        {
            _commonAreaService = commonAreaService;
        }

        /// <summary>
        /// Lấy danh sách khu vực chung có phân trang, tìm kiếm và sắp xếp.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> tất cả người dùng đã đăng nhập.<br/>
        /// <b>Tham số phân trang (<c>PaginateDto</c>):</b>
        /// <ul>
        ///   <li><b>page</b>: Số trang hiện tại (bắt đầu từ 1).</li>
        ///   <li><b>size</b>: Số bản ghi mỗi trang.</li>
        ///   <li><b>search</b>: Từ khóa tìm kiếm (theo tên, mã khu vực, mô tả, vị trí).</li>
        ///   <li><b>filter</b>: Lọc theo trạng thái khu vực (Active/Inactive).</li>
        ///   <li><b>sortBy</b>: Tiêu chí sắp xếp kết quả (name, name_desc, floor, floor_desc).</li>
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
        /// <b>IPaginate&lt;CommonAreaDto&gt;:</b>
        /// <ul>
        ///   <li><b>CommonAreaId</b>: ID khu vực chung.</li>
        ///   <li><b>FloorId</b>: ID tầng liên kết.</li>
        ///   <li><b>Floor</b>: Tên tầng liên kết.</li>
        ///   <li><b>AreaCode</b>: Mã khu vực.</li>
        ///   <li><b>Name</b>: Tên khu vực.</li>
        ///   <li><b>Description</b>: Mô tả khu vực.</li>
        ///   <li><b>Location</b>: Vị trí khu vực.</li>
        ///   <li><b>Status</b>: Trạng thái khu vực (Active/Inactive).</li>
        /// </ul>
        /// </returns>
        /// <response code="200">Trả về danh sách khu vực chung.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(IPaginate<CommonAreaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetPaginateCommonArea([FromQuery] PaginateDto dto)
        {
            var result = await _commonAreaService.GetPaginateCommonAreaAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của khu vực chung theo ID.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> tất cả người dùng đã đăng nhập.
        /// </remarks>
        /// <param name="id">ID của khu vực chung cần lấy thông tin.</param>
        /// <returns>
        /// <b>CommonAreaDto:</b>
        /// <ul>
        ///   <li><b>CommonAreaId</b>: ID khu vực chung.</li>
        ///   <li><b>FloorId</b>: ID tầng liên kết.</li>
        ///   <li><b>Floor</b>: Tên tầng liên kết.</li>
        ///   <li><b>AreaCode</b>: Mã khu vực.</li>
        ///   <li><b>Name</b>: Tên khu vực.</li>
        ///   <li><b>Description</b>: Mô tả khu vực.</li>
        ///   <li><b>Location</b>: Vị trí khu vực.</li>
        ///   <li><b>Status</b>: Trạng thái khu vực (Active/Inactive).</li>
        /// </ul>
        /// </returns>
        /// <response code="200">Trả về thông tin khu vực chung.</response>
        /// <response code="404">Không tìm thấy khu vực chung.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(CommonAreaDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetCommonAreaById(int id)
        {
            var result = await _commonAreaService.GetCommonAreaByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách khu vực chung.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> tất cả người dùng đã đăng nhập.
        /// </remarks>
        /// <returns>
        /// <b>FloorDto[]:</b>
        /// <ul>
        ///   <li><b>FloorId</b>: ID tầng.</li>
        ///   <li><b>FloorNumber</b>: Số thứ tự tầng.</li>
        ///   <li><b>Status</b>: Trạng thái tầng.</li>
        ///   <li><b>Description</b>: Mô tả tầng.</li>
        ///   <li><b>Apartments</b>: Danh sách căn hộ thuộc tầng.</li>
        ///   <li><b>CommonAreas</b>: Danh sách khu vực chung thuộc tầng.</li>
        /// </ul>
        /// </returns>
        /// <response code="200">Trả về danh sách khu vực chung.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet("list")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<FloorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetCommonAreas()
        {
            var result = await _commonAreaService.GetCommonAreasAsync();
            return Ok(result);
        }

        /// <summary>
        /// Tạo mới khu vực chung.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Manager<br/>
        /// <b>Dữ liệu yêu cầu (<c>CommonAreaCreateDto</c>):</b>
        /// <ul>
        ///   <li><b>FloorId</b>: ID tầng liên kết (tùy chọn).</li>
        ///   <li><b>AreaCode</b>: Mã khu vực (bắt buộc, tối đa 50 ký tự).</li>
        ///   <li><b>Name</b>: Tên khu vực (bắt buộc, tối đa 256 ký tự).</li>
        ///   <li><b>Description</b>: Mô tả khu vực (tối đa 500 ký tự, tùy chọn).</li>
        ///   <li><b>Location</b>: Vị trí khu vực (tối đa 500 ký tự, tùy chọn).</li>
        /// </ul>
        /// </remarks>
        /// <param name="dto">
        /// <b>CommonAreaCreateDto:</b>
        /// <ul>
        ///   <li><b>FloorId</b>: ID tầng liên kết.</li>
        ///   <li><b>AreaCode</b>: Mã khu vực.</li>
        ///   <li><b>Name</b>: Tên khu vực.</li>
        ///   <li><b>Description</b>: Mô tả khu vực.</li>
        ///   <li><b>Location</b>: Vị trí khu vực.</li>
        /// </ul>
        /// </param>
        /// <returns>Thông báo tạo khu vực chung thành công.</returns>
        /// <response code="201">Khu vực chung được tạo thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPost]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> CreateCommonArea(CommonAreaCreateDto dto)
        {
            var result = await _commonAreaService.CreateCommonAreaAsync(dto);
            return Created(string.Empty, result);
        }

        /// <summary>
        /// Cập nhật thông tin khu vực chung theo ID.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Manager<br/>
        /// <b>Dữ liệu cập nhật (<c>CommonAreaUpdateDto</c>):</b>
        /// <ul>
        ///   <li><b>FloorId</b>: ID tầng liên kết (tùy chọn).</li>
        ///   <li><b>AreaCode</b>: Mã khu vực (bắt buộc, tối đa 50 ký tự).</li>
        ///   <li><b>Name</b>: Tên khu vực (bắt buộc, tối đa 256 ký tự).</li>
        ///   <li><b>Description</b>: Mô tả khu vực (tối đa 500 ký tự, tùy chọn).</li>
        ///   <li><b>Location</b>: Vị trí khu vực (tối đa 500 ký tự, tùy chọn).</li>
        ///   <li><b>Status</b>: Trạng thái khu vực (Active/Inactive).</li>
        /// </ul>
        /// </remarks>
        /// <param name="id">ID của khu vực chung cần cập nhật.</param>
        /// <param name="dto">
        /// <b>CommonAreaUpdateDto:</b>
        /// <ul>
        ///   <li><b>FloorId</b>: ID tầng liên kết.</li>
        ///   <li><b>AreaCode</b>: Mã khu vực.</li>
        ///   <li><b>Name</b>: Tên khu vực.</li>
        ///   <li><b>Description</b>: Mô tả khu vực.</li>
        ///   <li><b>Location</b>: Vị trí khu vực.</li>
        ///   <li><b>Status</b>: Trạng thái khu vực.</li>
        /// </ul>
        /// </param>
        /// <returns>Không có nội dung trả về khi cập nhật thành công.</returns>
        /// <response code="204">Cập nhật khu vực chung thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="404">Không tìm thấy khu vực chung.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPut("{id:int}")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpdateCommonArea(int id, CommonAreaUpdateDto dto)
        {
            var result = await _commonAreaService.UpdateCommonAreaAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Xóa khu vực chung theo ID.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Manager<br/>
        /// Xóa khu vực chung sẽ không ảnh hưởng đến các dữ liệu khác ngoài quan hệ trực tiếp (nếu có).
        /// </remarks>
        /// <param name="id">ID của khu vực chung cần xóa.</param>
        /// <returns>Không có nội dung trả về khi xóa thành công.</returns>
        /// <response code="204">Khu vực chung được xóa thành công.</response>
        /// <response code="404">Không tìm thấy khu vực chung.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeleteCommonArea(int id)
        {
            var result = await _commonAreaService.DeleteCommonAreaAsync(id);
            return Ok(result);
        }
    }
}