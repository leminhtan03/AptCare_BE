using AptCare.Service.Dtos;
using AptCare.Service.Dtos.IssueDto;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{

    public class IssuaController : BaseApiController
    {
        private readonly IIssueService _issueService;

        public IssuaController(IIssueService issueService)
        {
            _issueService = issueService;
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] IssueCreateDto dto)
        {
            var result = await _issueService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.IssueId }, result);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _issueService.GetByIdAsync(id);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] PaginateDto q, [FromQuery] int? techniqueId = null)
        {
            var result = await _issueService.ListAsync(q, techniqueId);
            return Ok(result);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] IssueUpdateDto dto)
        {
            var result = await _issueService.UpdateAsync(id, dto);
            return Ok(result);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _issueService.DeleteAsync(id);
                return Ok("Issua " + id.ToString() + " dã bị vô hiệu hóa !!");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}