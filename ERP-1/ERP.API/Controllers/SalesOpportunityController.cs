using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ERP.API.Models;
using ERP.API.Models.DTOs;
using ERP.API.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;

namespace ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowAll")]
    [Produces("application/json")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public class SalesOpportunityController : ControllerBase
    {
        private readonly ISalesOpportunityService _opportunityService;
        private readonly ILogger<SalesOpportunityController> _logger;

        public SalesOpportunityController(
            ISalesOpportunityService opportunityService,
            ILogger<SalesOpportunityController> logger)
        {
            _opportunityService = opportunityService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all active sales opportunities
        /// </summary>
        /// <returns>List of all active sales opportunities</returns>
        /// <response code="200">Returns the list of opportunities</response>
        /// <response code="401">If the user is not authorized</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<SalesOpportunity>>> GetOpportunities()
        {
            try
            {
                var opportunities = await _opportunityService.GetOpportunitiesAsync();
                return Ok(opportunities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting opportunities: {Message}", ex.Message); return StatusCode(500, $"An error occurred while retrieving opportunities: {ex.Message}");
            }
        }        /// <summary>
                 /// Gets a specific sales opportunity by its opportunity ID
                 /// </summary>
                 /// <param name="opportunityId">The opportunity ID to retrieve (e.g., OPP00001)</param>
                 /// <returns>The requested sales opportunity</returns>
                 /// <response code="200">Returns the requested opportunity</response>
                 /// <response code="400">If the opportunity ID format is invalid</response>
                 /// <response code="404">If the opportunity is not found</response>
                 /// <response code="500">If there was an internal server error</response>
        [HttpGet("{opportunityId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SalesOpportunity>> GetOpportunity(string opportunityId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(opportunityId) || !opportunityId.StartsWith("OPP"))
                {
                    _logger.LogWarning("Invalid opportunity ID format: {Id}", opportunityId);
                    return BadRequest("Invalid opportunity ID format. Expected format: OPP followed by digits (e.g., OPP00001)");
                }

                var opportunity = await _opportunityService.GetOpportunityByIdAsync(opportunityId);

                if (opportunity == null)
                {
                    _logger.LogInformation("Opportunity not found: {Id}", opportunityId);
                    return NotFound($"Opportunity with ID {opportunityId} not found");
                }

                _logger.LogInformation("Retrieved opportunity: {Id}", opportunityId);
                return Ok(opportunity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving opportunity {OpportunityId}: {Message}", opportunityId, ex.Message);
                return StatusCode(500, new
                {
                    message = "Failed to retrieve opportunity",
                    error = ex.Message,
                    statusCode = 500
                });
            }
        }

        /// <summary>
        /// Gets a specific sales opportunity by its numeric database ID
        /// </summary>
        /// <param name="id">The numeric database ID of the opportunity</param>
        /// <returns>The requested sales opportunity</returns>
        /// <response code="200">Returns the requested opportunity</response>
        /// <response code="404">If the opportunity is not found</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("by-id/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SalesOpportunity>> GetOpportunityById(int id)
        {
            try
            {
                var opportunity = await _opportunityService.GetByIdAsync(id);
                if (opportunity == null)
                {
                    _logger.LogInformation("Opportunity not found by numeric ID: {Id}", id);
                    return NotFound($"Opportunity with numeric ID {id} not found");
                }
                _logger.LogInformation("Retrieved opportunity by numeric ID: {Id}", id);
                return Ok(opportunity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving opportunity by numeric ID {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new
                {
                    message = "Failed to retrieve opportunity by numeric ID",
                    error = ex.Message,
                    statusCode = 500
                });
            }
        }

        /// <summary>
        /// Gets all opportunities for a specific sales lead
        /// </summary>
        /// <param name="leadId">The ID of the sales lead</param>
        /// <returns>List of opportunities associated with the sales lead</returns>
        /// <response code="200">Returns the list of opportunities</response>
        /// <response code="400">If the lead ID is invalid</response>
        /// <response code="404">If no opportunities are found for the sales lead</response>
        /// <response code="401">If the user is not authorized</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("lead/{leadId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<SalesOpportunityDto>>> GetOpportunitiesByLeadId(int leadId)
        {
            try
            {
                if (leadId <= 0)
                {
                    _logger.LogWarning("Invalid lead ID: {LeadId}", leadId);
                    return BadRequest("Invalid lead ID. It must be a positive integer.");
                }

                var opportunities = await _opportunityService.GetOpportunitiesByLeadIdAsync(leadId);

                if (!opportunities.Any())
                {
                    _logger.LogInformation("No opportunities found for lead ID {LeadId}", leadId);
                    return NotFound($"No opportunities found for lead ID {leadId}");
                }

                _logger.LogInformation("Retrieved opportunities for lead ID {LeadId}", leadId);
                return Ok(opportunities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving opportunities for lead {LeadId}: {Message}", leadId, ex.Message); return StatusCode(500, new
                {
                    message = "An error occurred while retrieving opportunities",
                    error = ex.Message,
                    statusCode = 500
                });
            }
        }

        /// <summary>
        /// Gets opportunity counts grouped by status for the cards view
        /// </summary>
        /// <returns>Collection of opportunity cards with status, count, and total value</returns>
        /// <response code="200">Returns the opportunity cards data</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("cards")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<OpportunityCardDto>>> GetOpportunityCards()
        {
            try
            {
                var cards = await _opportunityService.GetOpportunityCardsAsync();
                return Ok(cards);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving opportunity cards: {Message}", ex.Message);
                return StatusCode(500, new
                {
                    message = "Failed to retrieve opportunity cards data",
                    error = ex.Message,
                    statusCode = 500
                });
            }
        }// POST: api/SalesOpportunity
        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        private string? CleanString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "string")
                return null;
            return value;
        }

        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> CreateOpportunity([FromBody] SalesOpportunity opportunity)
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

                // Clean string values before saving
                opportunity.OpportunityType = CleanString(opportunity.OpportunityType) ?? opportunity.OpportunityType;
                opportunity.OpportunityFor = CleanString(opportunity.OpportunityFor) ?? opportunity.OpportunityFor;
                opportunity.CustomerId = CleanString(opportunity.CustomerId);
                opportunity.CustomerType = CleanString(opportunity.CustomerType);
                opportunity.OpportunityName = CleanString(opportunity.OpportunityName) ?? opportunity.OpportunityName;
                opportunity.Comments = CleanString(opportunity.Comments);
                opportunity.LeadId = CleanString(opportunity.LeadId);
                opportunity.ContactName = CleanString(opportunity.ContactName);
                opportunity.ContactMobileNo = CleanString(opportunity.ContactMobileNo);

                var id = await _opportunityService.CreateOpportunityAsync(opportunity);
                _logger.LogInformation("Created new opportunity with ID {Id} and OpportunityId {OpportunityId}", id, opportunity.OpportunityId);
return Created($"api/SalesOpportunity/{opportunity.OpportunityId}", new { id, opportunityId = opportunity.OpportunityId });            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating opportunity: {Message}", ex.Message);
                if (ex is ArgumentException)
                {
                    return BadRequest(new
                    {
                        message = ex.Message,
                        statusCode = 400
                    });
                }
                return StatusCode(500, new
                {
                    message = "Failed to create opportunity",
                    error = ex.Message,
                    statusCode = 500
                });
            }
        }

        // PUT: api/SalesOpportunity/5
        /// <summary>
        /// Updates a specific sales opportunity
        /// </summary>
        /// <param name="opportunityId">The ID of the opportunity to update (e.g., OPP00001)</param>
        /// <param name="opportunity">The updated opportunity data</param>
        /// <returns>The updated opportunity</returns>
        /// <response code="200">Returns the updated opportunity</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="404">If the opportunity is not found</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpPut("{opportunityId}")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SalesOpportunity>> UpdateOpportunity([FromRoute] string opportunityId, [FromBody] SalesOpportunity opportunity)
        {
            if (opportunityId != opportunity.OpportunityId)
            {
                return BadRequest(new
                {
                    message = "Opportunity ID mismatch",
                    statusCode = 400
                });
            }
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

                // Clean string values before updating
                opportunity.OpportunityType = CleanString(opportunity.OpportunityType) ?? opportunity.OpportunityType;
                opportunity.OpportunityFor = CleanString(opportunity.OpportunityFor) ?? opportunity.OpportunityFor;
                opportunity.CustomerId = CleanString(opportunity.CustomerId);
                opportunity.CustomerType = CleanString(opportunity.CustomerType);
                opportunity.OpportunityName = CleanString(opportunity.OpportunityName) ?? opportunity.OpportunityName;
                opportunity.Comments = CleanString(opportunity.Comments);
                opportunity.LeadId = CleanString(opportunity.LeadId);
                opportunity.ContactName = CleanString(opportunity.ContactName);
                opportunity.ContactMobileNo = CleanString(opportunity.ContactMobileNo);

                var success = await _opportunityService.UpdateOpportunityAsync(opportunityId, opportunity);

                if (!success)
                {
                    _logger.LogWarning("Opportunity with ID {OpportunityId} not found for update", opportunityId);
                    return NotFound(new
                    {
                        message = $"Opportunity with ID {opportunityId} not found",
                        statusCode = 404
                    });
                }

                _logger.LogInformation("Updated opportunity with ID {OpportunityId}", opportunityId);
                // Get the updated opportunity and return it
                var updatedOpportunity = await _opportunityService.GetOpportunityByIdAsync(opportunity.OpportunityId);
                return Ok(updatedOpportunity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating opportunity {OpportunityId}: {ErrorMessage}", opportunityId, ex.Message);
                if (ex is ArgumentException)
                {
                    return BadRequest(new
                    {
                        message = ex.Message,
                        statusCode = 400
                    });
                }
                return StatusCode(500, new
                {
                    message = $"Failed to update opportunity {opportunityId}",
                    statusCode = 500,
                    errors = new[] { ex.Message }
                });
            }
        }        /// <summary>
                 /// Deletes a specific sales opportunity
                 /// </summary>
                 /// <param name="opportunityId">The ID of the opportunity to delete (e.g., OPP00001)</param>
                 /// <returns>No content on success</returns>
                 /// <response code="204">If the opportunity was successfully deleted</response>
                 /// <response code="400">If the opportunityId format is invalid</response>
                 /// <response code="404">If the opportunity is not found</response>
                 /// <response code="500">If there was an internal server error</response>
        [HttpDelete("{opportunityId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteOpportunity(string opportunityId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(opportunityId) || !opportunityId.StartsWith("OPP"))
                {
                    _logger.LogWarning("Invalid opportunity ID format: {OpportunityId}", opportunityId);
                    return BadRequest("Invalid opportunity ID format. Expected format: OPP followed by digits (e.g., OPP00001)");
                }

                var success = await _opportunityService.DeleteOpportunityAsync(opportunityId);

                if (!success)
                {
                    _logger.LogWarning("Opportunity with ID {OpportunityId} not found for deletion", opportunityId);
                    return NotFound($"Opportunity with ID {opportunityId} not found");
                }

                _logger.LogInformation("Deleted opportunity with ID {OpportunityId}", opportunityId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting opportunity {OpportunityId}: {Message}", opportunityId, ex.Message);
                return StatusCode(500, $"An error occurred while deleting the opportunity: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a paginated list of opportunities with filtering and sorting
        /// </summary>
        /// <param name="request">The grid request parameters</param>
        /// <returns>Paginated list of opportunities with total count</returns>
        [HttpPost("grid")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<(IEnumerable<SalesOpportunityGridResult> Results, int TotalRecords)>> GetOpportunitiesGrid([FromBody] SalesOpportunityGridRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Invalid request parameters",
                        statusCode = 400,
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var result = await _opportunityService.GetOpportunitiesGridAsync(
                    request.SearchText,
                    request.CustomerNames,
                    request.Territories,
                    request.Statuses,
                    request.Stages,
                    request.OpportunityTypes,
                    request.PageNumber ?? 1,
                    request.PageSize ?? 10,
                    request.OrderBy,
                    request.OrderDirection);

                return Ok(new
                {
                    Results = result.Results,
                    TotalRecords = result.TotalRecords,
                    PageNumber = request.PageNumber ?? 1,
                    PageSize = request.PageSize ?? 10
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting opportunities grid: {Message}", ex.Message);
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving opportunities",
                    statusCode = 500,
                    errors = new[] { ex.Message }
                });
            }
        }
    }
}
