using AptCare.Service.Dtos.FeedbackDtos;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AptCare.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;

        public FeedbackController(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        /// <summary>
        /// Tạo feedback mới hoặc reply feedback.
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b> Tạo mới một feedback cho yêu cầu sửa chữa hoặc trả lời (reply) một feedback đã có.<br/>
        /// <b>Tham số (<c>CreateFeedbackRequest</c>):</b>
        /// <ul>
        ///   <li><b>RepairRequestId</b>: ID của yêu cầu sửa chữa liên kết (bắt buộc).</li>
        ///   <li><b>ParentFeedbackId</b>: ID feedback cha (nếu là reply, tùy chọn).</li>
        ///   <li><b>Rating</b>: Điểm đánh giá (bắt buộc, ví dụ: 1-5).</li>
        ///   <li><b>Comment</b>: Nội dung bình luận (bắt buộc).</li>
        /// </ul>
        /// </remarks>
        /// <param name="request">
        /// <b>CreateFeedbackRequest:</b>
        /// <ul>
        ///   <li><b>RepairRequestId</b>: ID yêu cầu sửa chữa liên kết.</li>
        ///   <li><b>ParentFeedbackId</b>: ID feedback cha (nếu là reply).</li>
        ///   <li><b>Rating</b>: Điểm đánh giá.</li>
        ///   <li><b>Comment</b>: Nội dung bình luận.</li>
        /// </ul>
        /// </param>
        /// <returns>Thông tin feedback vừa tạo hoặc trả lời.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateFeedback([FromBody] CreateFeedbackRequest request)
        {
            try
            {
                var result = await _feedbackService.CreateFeedbackAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy toàn bộ chuỗi feedback của một repair request.
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b> Lấy danh sách tất cả feedback và các reply liên quan đến một yêu cầu sửa chữa.<br/>
        /// <b>Tham số:</b>
        /// <ul>
        ///   <li><b>repairRequestId</b>: ID của yêu cầu sửa chữa cần lấy feedback.</li>
        /// </ul>
        /// </remarks>
        /// <param name="repairRequestId">ID yêu cầu sửa chữa.</param>
        /// <returns>Danh sách chuỗi feedback của yêu cầu sửa chữa.</returns>
        [HttpGet("repair-request/{repairRequestId}")]
        public async Task<IActionResult> GetFeedbackThread(int repairRequestId)
        {
            try
            {
                var result = await _feedbackService.GetFeedbackThreadAsync(repairRequestId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy chi tiết một feedback theo ID.
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b> Lấy thông tin chi tiết của một feedback, bao gồm nội dung, điểm đánh giá, và các reply (nếu có).<br/>
        /// <b>Tham số:</b>
        /// <ul>
        ///   <li><b>feedbackId</b>: ID của feedback cần lấy chi tiết.</li>
        /// </ul>
        /// </remarks>
        /// <param name="feedbackId">ID của feedback cần lấy chi tiết.</param>
        /// <returns>Thông tin chi tiết của feedback.</returns>
        [HttpGet("{feedbackId}")]
        public async Task<IActionResult> GetFeedbackById(int feedbackId)
        {
            try
            {
                var result = await _feedbackService.GetFeedbackByIdAsync(feedbackId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa feedback và tất cả replies của nó.
        /// </summary>
        /// <remarks>
        /// <b>Chức năng:</b> Xóa một feedback và toàn bộ các reply liên quan.<br/>
        /// <b>Tham số:</b>
        /// <ul>
        ///   <li><b>feedbackId</b>: ID của feedback cần xóa.</li>
        /// </ul>
        /// </remarks>
        /// <param name="feedbackId">ID của feedback cần xóa.</param>
        /// <returns>Thông báo xóa thành công.</returns>
        [HttpDelete("{feedbackId}")]
        public async Task<IActionResult> DeleteFeedback(int feedbackId)
        {
            try
            {
                var result = await _feedbackService.DeleteFeedbackAsync(feedbackId);
                return Ok(new { success = result, message = "Xóa feedback thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}