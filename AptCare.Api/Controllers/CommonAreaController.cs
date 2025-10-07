using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.BuildingDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{
    //[Authorize(Roles = nameof(AccountRole.Manager))]
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
        [HttpGet]
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
        [HttpGet("{id}")]
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
        [HttpPost]
        public async Task<ActionResult> CreateCommonArea(CommonAreaCreateDto dto)
        {
            var result = await _commonAreaService.CreateCommonAreaAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật thông tin khu vực chung theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager  
        ///  
        /// Cập nhật thông tin như: mã khu vực, tên, mô tả, vị trí, trạng thái, ID tầng.
        /// </remarks>
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateCommonArea(int id, CommonAreaUpdateDto dto)
        {
            var result = await _commonAreaService.UpdateCommonAreaAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Xóa khu vực chung theo ID.
        /// </summary>
        /// <remarks>
        /// **Chỉ role:** Manager  
        ///  
        /// Xóa khu vực chung sẽ không ảnh hưởng đến các dữ liệu khác ngoài quan hệ trực tiếp (nếu có).
        /// </remarks>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCommonArea(int id)
        {
            var result = await _commonAreaService.DeleteCommonAreaAsync(id);
            return Ok(result);
        }
    }
}
