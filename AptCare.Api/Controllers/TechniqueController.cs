using Microsoft.AspNetCore.Mvc;
using AptCare.Service.Services.Interfaces;
using AptCare.Service.Dtos.TechniqueDto;
using AptCare.Service.Dtos;
using AptCare.Repository.Paginate;

namespace AptCare.Api.Controllers
{
    public class TechniqueController : BaseApiController
    {
        private readonly ITechniqueService _techniqueService;
        public TechniqueController(ITechniqueService techniqueService)
        {
            _techniqueService = techniqueService;
        }
        /// <summary>
        /// Gets a technique by its unique identifier
        /// </summary>
        /// <remarks>
        /// This endpoint fetches detailed technique information including name, description, and related statistics.
        /// Returns 404 if the technique is not found, or 500 if an internal error occurs.
        /// </remarks>
        /// <param name="id">The unique identifier of the technique</param>
        /// <returns>
        /// Returns an ActionResult containing:
        /// - 200 OK with TechniqueListItemDto object if technique is found
        /// - 404 Not Found if technique doesn't exist
        /// - 500 Internal Server Error if an exception occurs
        /// </returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(TechniqueListItemDto), 200)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _techniqueService.GetByIdAsync(id);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }
        /// <summary>
        /// Creates a new technique
        /// </summary>
        /// <remarks>
        /// This endpoint creates a new technique with the provided details.
        /// Returns 201 Created with the newly created technique if successful, or 400 Bad Request if validation fails.
        /// </remarks>
        /// <param name="dto">The technique creation data transfer object containing name and optional description</param>
        /// <returns>
        /// Returns an ActionResult containing:
        /// - 201 Created with TechniqueListItemDto if creation is successful
        /// - 400 Bad Request if the input data is invalid
        /// - 500 Internal Server Error if an exception occurs
        /// </returns>
        [HttpPost]
        [ProducesResponseType(typeof(TechniqueListItemDto), 201)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] TechniqueCreateDto dto)
        {
            var result = await _techniqueService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.TechniqueId }, result);
        }
        /// <summary>
        /// Updates an existing technique
        /// </summary>
        /// <remarks>
        /// This endpoint updates an existing technique with the provided details.
        /// Returns 200 OK with the updated technique if successful, or 404 Not Found if the technique doesn't exist.
        /// </remarks>
        /// <param name="id">The unique identifier of the technique to update</param>
        /// <param name="dto">The technique update data transfer object containing updated name and optional description</param>
        /// <returns>
        /// Returns an ActionResult containing:
        /// - 200 OK with TechniqueListItemDto if update is successful
        /// - 404 Not Found if the technique doesn't exist
        /// - 500 Internal Server Error if an exception occurs
        /// </returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(TechniqueListItemDto), 200)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] TechniqueUpdateDto dto)
        {
            var result = await _techniqueService.UpdateAsync(id, dto);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }
        /// <summary>
        /// Retrieves a paginated list of techniques
        /// </summary>
        /// <remarks>
        /// This endpoint fetches a paginated list of techniques based on the provided query parameters.
        /// Supports pagination, sorting, searching, and filtering of techniques.
        /// </remarks>
        /// <param name="query">Pagination parameters including page number, size, sortBy, search, and filter options</param>
        /// <returns>
        /// Returns an ActionResult containing:
        /// - 200 OK with a paginated list of TechniqueListItemDto objects
        /// - 500 Internal Server Error if an exception occurs
        /// </returns>
        [HttpGet]
        [ProducesResponseType(typeof(IPaginate<TechniqueListItemDto>), 200)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> List([FromQuery] PaginateDto query)
        {
            var result = await _techniqueService.ListAsync(query);
            return Ok(result);
        }
    }
}
