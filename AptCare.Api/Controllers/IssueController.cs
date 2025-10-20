using AptCare.Repository.Paginate;
using AptCare.Service.Dtos;
using AptCare.Service.Dtos.IssueDto;
using AptCare.Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AptCare.Api.Controllers
{

    public class IssueController : BaseApiController
    {
        private readonly IIssueService _issueService;

        public IssueController(IIssueService issueService)
        {
            _issueService = issueService;
        }
        /// <summary>
        /// Creates a new issue in the system
        /// </summary>
        /// <remarks>
        /// This endpoint creates a new issue with the provided details.
        /// The issue will be assigned a unique identifier automatically.
        /// </remarks>
        /// <param name="dto">The data transfer object containing issue details like technique ID, name, description, and other required properties</param>
        /// <returns>
        /// Returns an ActionResult containing:
        /// - 201 Created with the newly created issue if successful
        /// - 400 Bad Request if the model is invalid
        /// - 500 Internal Server Error if an exception occurs
        /// </returns>
        [HttpPost]
        [ProducesResponseType(typeof(IssueListItemDto), 201)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] IssueCreateDto dto)
        {
            var result = await _issueService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.IssueId }, result);
        }
        /// <summary>
        /// Retrieves a specific issue by its identifier
        /// </summary>
        /// <remarks>
        /// This endpoint fetches an issue with the specified ID from the system.
        /// It returns the complete issue details if found.
        /// </remarks>
        /// <param name="id">The unique identifier of the issue to retrieve</param>
        /// <returns>
        /// Returns an ActionResult containing:
        /// - 200 OK with the issue details if found
        /// - 404 Not Found if no issue exists with the specified ID
        /// </returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(IssueListItemDto), 200)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]

        public async Task<IActionResult> GetById(int id)
        {
            var result = await _issueService.GetByIdAsync(id);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }
        /// <summary>
        /// Retrieves a paginated list of issues with optional filtering
        /// </summary>
        /// <remarks>
        /// This endpoint returns a paginated list of issues from the system.
        /// The results can be filtered by technique ID, sorted, and searched based on the pagination parameters.
        /// </remarks>
        /// <param name="q">Pagination parameters including page number, page size, sorting, and search criteria</param>
        /// <param name="techniqueId">Optional filter to return only issues associated with a specific technique</param>
        /// <returns>
        /// Returns an ActionResult containing:
        /// - 200 OK with the paginated list of issues
        /// - 400 Bad Request if the pagination parameters are invalid
        /// </returns>
        [HttpGet]
        [ProducesResponseType(typeof(IPaginate<IssueListItemDto>), 200)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> List([FromQuery] PaginateDto q, [FromQuery] int? techniqueId = null)
        {
            var result = await _issueService.ListAsync(q, techniqueId);
            return Ok(result);
        }
        /// <summary>
        /// Updates an existing issue in the system
        /// </summary>
        /// <remarks>
        /// This endpoint updates an existing issue with the specified ID using the provided details.
        /// All properties in the DTO will replace the corresponding properties of the existing issue.
        /// </remarks>
        /// <param name="id">The unique identifier of the issue to update</param>
        /// <param name="dto">The data transfer object containing the updated issue details</param>
        /// <returns>
        /// Returns an ActionResult containing:
        /// - 200 OK with the result of the update operation if successful
        /// - 400 Bad Request if the model is invalid or the issue doesn't exist
        /// - 500 Internal Server Error if an exception occurs
        /// </returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(IssueListItemDto), 200)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] IssueUpdateDto dto)
        {
            var result = await _issueService.UpdateAsync(id, dto);
            return Ok(result);
        }
        /// <summary>
        /// Deletes or deactivates an issue from the system
        /// </summary>
        /// <remarks>
        /// This endpoint disables an issue with the specified ID in the system.
        /// The issue is not physically removed but rather marked as inactive.
        /// </remarks>
        /// <param name="id">The unique identifier of the issue to delete</param>
        /// <returns>
        /// Returns an ActionResult containing:
        /// - 200 OK with a success message if the issue is successfully deactivated
        /// - 400 Bad Request with an error message if the operation fails
        /// </returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
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