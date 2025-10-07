using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    //[Authorize(Roles = nameof(AccountRole.Manager))] // Áp dụng cho toàn bộ controller
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
        /// **Chỉ role:** Manager  
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
        public async Task<ActionResult> GetPaginateFloor([FromQuery] PaginateDto dto)
        {
            var result = await _floorService.GetPaginateFloorAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một tầng theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager
        /// </remarks>
        /// <param name="id">ID của tầng cần lấy thông tin.</param>
        /// <returns>Thông tin chi tiết của tầng.</returns>
        /// <response code="200">Trả về thông tin tầng.</response>
        /// <response code="404">Không tìm thấy tầng.</response>
        [HttpGet("{id}")]
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
        /// Dữ liệu yêu cầu gồm: số tầng, mô tả, trạng thái, ID tòa nhà, v.v.
        /// </remarks>
        /// <param name="dto">Thông tin tầng cần tạo.</param>
        /// <returns>Thông báo tạo tầng thành công.</returns>
        /// <response code="200">Tầng được tạo thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ.</response>
        [HttpPost]
        public async Task<ActionResult> CreateFloor(FloorCreateDto dto)
        {
            var result = await _floorService.CreateFloorAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật thông tin tầng theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager  
        ///  
        /// Cập nhật thông tin như: mô tả, trạng thái, hoặc số tầng.
        /// </remarks>
        /// <param name="id">ID của tầng cần cập nhật.</param>
        /// <param name="dto">Thông tin tầng cập nhật.</param>
        /// <returns>Thông báo cập nhật thành công.</returns>
        /// <response code="200">Cập nhật tầng thành công.</response>
        /// <response code="404">Không tìm thấy tầng.</response>
        [HttpPut("{id}")]
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
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteFloor(int id)
        {
            var result = await _floorService.DeleteFloorAsync(id);
            return Ok(result);
        }
    }
}
