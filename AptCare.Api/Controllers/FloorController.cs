using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    public class FloorController : BaseApiController
    {
        private readonly IFloorService _floorService;

        public FloorController(IFloorService floorService)
        {
            _floorService = floorService;
        }

        /// <summary>
        /// Lấy danh sách tầng có phân trang, tìm kiếm và sắp xếp.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> tất cả người dùng đã đăng nhập.<br/>
        /// <b>Tham số phân trang (<c>PaginateDto</c>):</b>
        /// <ul>
        ///   <li><b>page</b>: Số trang hiện tại (bắt đầu từ 1).</li>
        ///   <li><b>size</b>: Số bản ghi mỗi trang.</li>
        ///   <li><b>search</b>: Từ khóa tìm kiếm (theo số tầng hoặc mô tả).</li>
        ///   <li><b>filter</b>: Lọc theo trạng thái tầng.</li>
        ///   <li><b>sortBy</b>: Tiêu chí sắp xếp (floor, floor_desc).</li>
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
        /// <b>IPaginate&lt;GetAllFloorsDto&gt;:</b>
        /// <ul>
        ///   <li><b>FloorId</b>: ID tầng.</li>
        ///   <li><b>FloorNumber</b>: Số thứ tự tầng.</li>
        ///   <li><b>Status</b>: Trạng thái tầng.</li>
        ///   <li><b>Description</b>: Mô tả tầng.</li>
        /// </ul>
        /// </returns>
        /// <response code="200">Trả về danh sách tầng.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(IPaginate<GetAllFloorsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetPaginateFloor([FromQuery] PaginateDto dto)
        {
            var result = await _floorService.GetPaginateFloorAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách tầng.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> tất cả người dùng đã đăng nhập.
        /// </remarks>
        /// <returns>
        /// <b>FloorBasicDto[]:</b>
        /// <ul>
        ///   <li><b>FloorId</b>: ID tầng.</li>
        ///   <li><b>FloorNumber</b>: Số thứ tự tầng.</li>
        ///   <li><b>Status</b>: Trạng thái tầng.</li>
        ///   <li><b>Description</b>: Mô tả tầng.</li>
        /// </ul>
        /// </returns>
        /// <response code="200">Trả về danh sách tầng.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet("list")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<FloorBasicDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetFloors()
        {
            var result = await _floorService.GetFloorsAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một tầng theo ID.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> tất cả người dùng đã đăng nhập.
        /// </remarks>
        /// <param name="id">ID của tầng cần lấy thông tin.</param>
        /// <returns>
        /// <b>FloorDto:</b>
        /// <ul>
        ///   <li><b>FloorId</b>: ID tầng.</li>
        ///   <li><b>FloorNumber</b>: Số thứ tự tầng.</li>
        ///   <li><b>Status</b>: Trạng thái tầng.</li>
        ///   <li><b>Description</b>: Mô tả tầng.</li>
        ///   <li><b>Apartments</b>: Danh sách căn hộ thuộc tầng.</li>
        ///   <li><b>CommonAreas</b>: Danh sách khu vực chung thuộc tầng.</li>
        /// </ul>
        /// </returns>
        /// <response code="200">Trả về thông tin tầng.</response>
        /// <response code="404">Không tìm thấy tầng.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(FloorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetFloorById(int id)
        {
            var result = await _floorService.GetFloorByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Tạo mới một tầng.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Manager<br/>
        /// <b>Dữ liệu yêu cầu (<c>FloorCreateDto</c>):</b>
        /// <ul>
        ///   <li><b>FloorNumber</b>: Số thứ tự tầng (bắt buộc, từ 1 đến 100).</li>
        ///   <li><b>Description</b>: Mô tả tầng (bắt buộc).</li>
        /// </ul>
        /// </remarks>
        /// <param name="dto">
        /// <b>FloorCreateDto:</b>
        /// <ul>
        ///   <li><b>FloorNumber</b>: Số thứ tự tầng.</li>
        ///   <li><b>Description</b>: Mô tả tầng.</li>
        /// </ul>
        /// </param>
        /// <returns>Thông báo tạo tầng thành công.</returns>
        /// <response code="201">Tầng được tạo thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPost]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> CreateFloor(FloorCreateDto dto)
        {
            var result = await _floorService.CreateFloorAsync(dto);
            return Created(string.Empty, result);
        }

        /// <summary>
        /// Cập nhật thông tin tầng theo ID.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Manager<br/>
        /// <b>Dữ liệu cập nhật (<c>FloorUpdateDto</c>):</b>
        /// <ul>
        ///   <li><b>FloorNumber</b>: Số thứ tự tầng (bắt buộc, từ 1 đến 100).</li>
        ///   <li><b>Description</b>: Mô tả tầng (bắt buộc).</li>
        ///   <li><b>Status</b>: Trạng thái tầng (Active/Inactive).</li>
        /// </ul>
        /// </remarks>
        /// <param name="id">ID của tầng cần cập nhật.</param>
        /// <param name="dto">
        /// <b>FloorUpdateDto:</b>
        /// <ul>
        ///   <li><b>FloorNumber</b>: Số thứ tự tầng.</li>
        ///   <li><b>Description</b>: Mô tả tầng.</li>
        ///   <li><b>Status</b>: Trạng thái tầng.</li>
        /// </ul>
        /// </param>
        /// <returns>Thông báo cập nhật thành công.</returns>
        /// <response code="200">Cập nhật tầng thành công.</response>
        /// <response code="404">Không tìm thấy tầng.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPut("{id}")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpdateFloor(int id, FloorUpdateDto dto)
        {
            var result = await _floorService.UpdateFloorAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Xóa một tầng theo ID.
        /// </summary>
        /// <remarks>
        /// <b>Chỉ role:</b> Manager<br/>
        /// Xóa tầng sẽ không ảnh hưởng đến các dữ liệu khác ngoài quan hệ trực tiếp (nếu có).
        /// </remarks>
        /// <param name="id">ID của tầng cần xóa.</param>
        /// <returns>Thông báo xóa thành công.</returns>
        /// <response code="200">Tầng được xóa thành công.</response>
        /// <response code="404">Không tìm thấy tầng.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = nameof(AccountRole.Manager))]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeleteFloor(int id)
        {
            var result = await _floorService.DeleteFloorAsync(id);
            return Ok(result);
        }
    }
}