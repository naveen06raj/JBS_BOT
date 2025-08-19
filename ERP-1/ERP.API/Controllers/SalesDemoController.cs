using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ERP.API.Models;
using ERP.API.Models.DTOs;
using ERP.API.Services;

namespace ERP.API.Controllers
{    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowAll")]
    [Produces("application/json")]
    [ApiExplorerSettings(IgnoreApi = false)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public class SalesDemoController : ControllerBase
    {
        private readonly ISalesDemoService _demoService;
        private readonly ILogger<SalesDemoController> _logger;

        public SalesDemoController(
            ISalesDemoService demoService,
            ILogger<SalesDemoController> logger)
        {
            _demoService = demoService;
            _logger = logger;        }

        /// <summary>
        /// Gets all sales demos.
        /// </summary>
        /// <returns>A list of all sales demos</returns>
        /// <response code="200">Returns the list of demos</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SalesDemo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<SalesDemo>>> GetDemos()
        {
            try
            {
                var demos = await _demoService.GetDemosAsync();
                return Ok(demos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting demos: {Message}", ex.Message);
                return StatusCode(500, $"Failed to retrieve demos: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a specific sales demo by ID.
        /// </summary>
        /// <param name="id">The ID of the sales demo to retrieve</param>
        /// <returns>The requested sales demo</returns>
        /// <response code="200">Returns the requested demo</response>
        /// <response code="400">If the ID is invalid</response>
        /// <response code="404">If the demo was not found</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SalesDemo), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SalesDemo>> GetDemo(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid demo ID: {Id}", id);
                    return BadRequest("Invalid demo ID");
                }

                var demo = await _demoService.GetDemoByIdAsync(id);
                
                if (demo == null)
                {
                    _logger.LogInformation("Demo not found: {Id}", id);
                    return NotFound($"Demo with ID {id} not found");
                }

                return Ok(demo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving demo {Id}: {Message}", id, ex.Message);
                return StatusCode(500, $"Failed to retrieve demo {id}: {ex.Message}");
            }
        }        /// <summary>        /// Creates a new sales demo record.
        /// </summary>
        /// <param name="demoDto">The sales demo record to create</param>
        /// <returns>The ID of the newly created demo</returns>
        /// <response code="201">Returns the newly created demo ID</response>
        /// <response code="400">If the demo data is invalid</response>
        /// <response code="401">If the user is not authorized</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> CreateDemo([FromBody] CreateSalesDemoDto demoDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Invalid model state",
                        statusCode = 400,
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                var demo = new SalesDemo
                {
                    CustomerName = demoDto.CustomerName ?? string.Empty,
                    DemoName = demoDto.DemoName ?? string.Empty,
                    Status = demoDto.Status ?? "Pending",
                    DemoDate = DateTime.UtcNow, // default to current time if not provided
                    DemoContact = demoDto.DemoContact ?? string.Empty,
                    DemoApproach = demoDto.DemoApproach ?? string.Empty,
                    DemoOutcome = demoDto.DemoOutcome ?? string.Empty,
                    DemoFeedback = demoDto.DemoFeedback ?? string.Empty,
                    Comments = demoDto.Comments ?? string.Empty,
                    UserId = demoDto.UserId,
                    AddressId = demoDto.AddressId,
                    OpportunityId = demoDto.OpportunityId,
                    CustomerId = demoDto.CustomerId,
                    PresenterId = demoDto.PresenterId,                    DateCreated = DateTime.UtcNow,
                    UserCreated = 1 // Default user ID since we're removing auth requirements
                };

                var id = await _demoService.CreateDemoAsync(demo);
                _logger.LogInformation("Created new demo with ID {Id}", id);

                return CreatedAtAction(nameof(GetDemo), new { id }, id);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input for demo creation: {Message}", ex.Message);
                return BadRequest(new
                {
                    message = ex.Message,
                    statusCode = 400
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating demo: {Message}", ex.Message);
                return StatusCode(500, new
                {
                    message = "Failed to create demo",
                    error = ex.Message,
                    statusCode = 500
                });
            }
        }   
         [HttpGet("cards")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DemoCardsDto>> GetDemoCards()
        {
            try
            {
                var cards = await _demoService.GetDemoCardsAsync();
                return Ok(cards);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Failed to retrieve Demo cards data",
                    error = ex.Message,
                    statusCode = 500 
                });
            }
        }

             /// <summary>
        /// Updates an existing sales demo
        /// </summary>
        /// <param name="id">The ID of the demo to update</param>
        /// <param name="demoDto">The updated demo information</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">If the update was successful</response>
        /// <response code="400">If the demo data is invalid</response>
        /// <response code="401">If the user is not authorized</response>
        /// <response code="404">If the demo is not found</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpPut("{id}")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateDemo([FromRoute] int id, [FromBody] UpdateSalesDemoDto demoDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Invalid model state",
                        statusCode = 400,
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                var demo = new SalesDemo
                {
                    Id = id,
                    CustomerName = demoDto.CustomerName,
                    DemoName = demoDto.DemoName,
                    Status = demoDto.Status,
                    DemoDate = demoDto.DemoDate,
                    DemoContact = demoDto.DemoContact,
                    DemoApproach = demoDto.DemoApproach,
                    DemoOutcome = demoDto.DemoOutcome ?? string.Empty,
                    DemoFeedback = demoDto.DemoFeedback ?? string.Empty,
                    Comments = demoDto.Comments ?? string.Empty,
                    UserId = demoDto.UserId,
                    AddressId = demoDto.AddressId,
                    OpportunityId = demoDto.OpportunityId,
                    CustomerId = demoDto.CustomerId,
                    PresenterId = demoDto.PresenterId,                    DateUpdated = DateTime.UtcNow,
                    UserUpdated = 1 // Default user ID since we're removing auth requirements
                };

                var success = await _demoService.UpdateDemoAsync(id, demo);

                if (!success)
                {
                    _logger.LogWarning("Demo with ID {Id} not found for update", id);
                    return NotFound(new { message = $"Demo with ID {id} not found", statusCode = 404 });
                }

                _logger.LogInformation("Updated demo with ID {Id}", id);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input for demo update: {Message}", ex.Message);
                return BadRequest(new
                {
                    message = ex.Message,
                    statusCode = 400
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating demo {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new
                {
                    message = $"Failed to update demo {id}",
                    error = ex.Message,
                    statusCode = 500
                });
            }
        }

        // DELETE: api/SalesDemo/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDemo(int id)
        {
            try
            {
                var success = await _demoService.DeleteDemoAsync(id);

                if (!success)
                {
                    _logger.LogWarning("Demo with ID {Id} not found for deletion", id);
                    return NotFound($"Demo with ID {id} not found");
                }

                _logger.LogInformation("Soft deleted demo with ID {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting demo {Id}: {Message}", id, ex.Message);
                return StatusCode(500, $"Failed to delete demo {id}: {ex.Message}");
            }
        }

        // GET: api/SalesDemo/opportunity/5
        [HttpGet("opportunity/{opportunityId}")]
        public async Task<ActionResult<IEnumerable<SalesDemo>>> GetDemosByOpportunity(int opportunityId)
        {
            try
            {
                if (opportunityId <= 0)
                {
                    _logger.LogWarning("Invalid opportunity ID: {Id}", opportunityId);
                    return BadRequest("Invalid opportunity ID");
                }

                var demos = await _demoService.GetDemosByOpportunityIdAsync(opportunityId);
                return Ok(demos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving demos for opportunity {Id}: {Message}", opportunityId, ex.Message);
                return StatusCode(500, $"Failed to retrieve demos for opportunity {opportunityId}: {ex.Message}");
            }
        }
    }
}
