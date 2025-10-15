using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    [Authorize(Roles = nameof(AccountRole.Manager))]
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
        /// **Chỉ role:** Manager  
        ///  
        /// **Tham số phân trang (PaginateDto):**
        /// - <b>page</b>: Số trang hiện tại (bắt đầu từ 1).  
        /// - <b>size</b>: Số bản ghi mỗi trang.  
        /// - <b>search</b>: Từ khóa tìm kiếm (theo tên, mã khu vực, mô tả, vị trí).  
        /// - <b>filter</b>: Lọc theo trạng thái khu vực (Active/Inactive).  
        /// - <b>sortBy</b>: Tiêu chí sắp xếp kết quả:
        ///   - <b>name</b>: Theo tên tăng dần.  
        ///   - <b>name_desc</b>: Theo tên giảm dần.  
        ///   - <b>floor</b>: Theo tầng tăng dần.  
        ///   - <b>floor_desc</b>: Theo tầng giảm dần.  
        /// </remarks>
        /// <param name="dto">Thông tin phân trang, tìm kiếm, và sắp xếp.</param>
        /// <returns>Danh sách khu vực chung kèm thông tin phân trang.</returns>
        /// <response code="200">Trả về danh sách khu vực chung.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IPaginate<CommonAreaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GetPaginateCommonArea([FromQuery] PaginateDto dto)
        {
            var result = await _commonAreaService.GetPaginateCommonAreaAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của khu vực chung theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager
        /// </remarks>
        /// <param name="id">ID của khu vực chung cần lấy thông tin.</param>
        /// <returns>Thông tin chi tiết của khu vực chung.</returns>
        /// <response code="200">Trả về thông tin khu vực chung.</response>
        /// <response code="404">Không tìm thấy khu vực chung.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CommonAreaDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GetCommonAreaById(int id)
        {
            var result = await _commonAreaService.GetCommonAreaByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Tạo mới khu vực chung.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager  
        ///  
        /// Dữ liệu yêu cầu gồm: mã khu vực, tên, vị trí, mô tả, ID tầng.
        /// </remarks>
        /// <param name="dto">Thông tin khu vực chung cần tạo.</param>
        /// <returns>Thông báo tạo khu vực chung thành công.</returns>
        /// <response code="201">Khu vực chung được tạo thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPost]
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
        /// **Chỉ role:** Manager  
        ///  
        /// Cập nhật thông tin như: mã khu vực, tên, mô tả, vị trí, trạng thái, ID tầng.
        /// </remarks>
        /// <param name="id">ID của khu vực chung cần cập nhật.</param>
        /// <param name="dto">Thông tin khu vực chung cập nhật.</param>
        /// <returns>Không có nội dung trả về khi cập nhật thành công.</returns>
        /// <response code="204">Cập nhật khu vực chung thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        /// <response code="404">Không tìm thấy khu vực chung.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpdateCommonArea(int id, CommonAreaUpdateDto dto)
        {
            await _commonAreaService.UpdateCommonAreaAsync(id, dto);
            return NoContent();
        }

        /// <summary>
        /// Xóa khu vực chung theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager  
        ///  
        /// Xóa khu vực chung sẽ không ảnh hưởng đến các dữ liệu khác ngoài quan hệ trực tiếp (nếu có).
        /// </remarks>
        /// <param name="id">ID của khu vực chung cần xóa.</param>
        /// <returns>Không có nội dung trả về khi xóa thành công.</returns>
        /// <response code="204">Khu vực chung được xóa thành công.</response>
        /// <response code="404">Không tìm thấy khu vực chung.</response>
        /// <response code="401">Không có quyền truy cập.</response>
        /// <response code="403">Không đủ quyền truy cập.</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeleteCommonArea(int id)
        {
            await _commonAreaService.DeleteCommonAreaAsync(id);
            return NoContent();
        }
    }
}