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
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.  
        ///  
        /// **Tham số phân trang (PaginateDto):**
        /// - <b>page</b>: Số trang hiện tại (bắt đầu từ 1).  
        /// - <b>size</b>: Số bản ghi mỗi trang.  
        /// - <b>search</b>: Từ khóa tìm kiếm (theo số tầng hoặc mô tả).  
        /// - <b>filter</b>: Lọc theo trạng thái tầng.  
        /// - <b>sortBy</b>: Tiêu chí sắp xếp kết quả:
        ///   - <b>floor</b>: Sắp xếp theo số tầng tăng dần.  
        ///   - <b>floor_desc</b>: Sắp xếp theo số tầng giảm dần.    
        /// </remarks>
        /// <param name="dto">Thông tin phân trang, tìm kiếm, sắp xếp và lọc.</param>
        /// <returns>Danh sách tầng kèm thông tin phân trang.</returns>
        /// <response code="200">Trả về danh sách tầng.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(IPaginate<FloorDto>), StatusCodes.Status200OK)]
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
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.  
        ///    
        /// </remarks>
        /// <returns>Danh sách tầng.</returns>
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
        /// **Chỉ role:** tất cả người dùng đã đăng nhập.  
        /// </remarks>
        /// <param name="id">ID của tầng cần lấy thông tin.</param>
        /// <returns>Thông tin chi tiết của tầng.</returns>
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
        /// **Chỉ role:** Manager  
        ///  
        /// Dữ liệu yêu cầu gồm: số tầng, mô tả, Mã tòa nhà, v.v.
        /// </remarks>
        /// <param name="dto">Thông tin tầng cần tạo.</param>
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
        /// **Chỉ role:** Manager  
        ///  
        /// Cập nhật thông tin như: mô tả, trạng thái, Mã tòa nhà, hoặc số tầng.
        /// </remarks>
        /// <param name="id">ID của tầng cần cập nhật.</param>
        /// <param name="dto">Thông tin tầng cập nhật.</param>
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
        /// **Chỉ role:** Manager  
        ///  
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