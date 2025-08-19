using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;
using System.Threading.Tasks;
using ERP.API.Services;
using ERP.API.Models.DTOs;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ERP.API.Controllers
{      /// <summary>
    /// Controller for managing quotations
    /// </summary>
    [ApiController]
    [Route("api/quotations")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status406NotAcceptable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    
    public class QuotationController : ControllerBase
    {
        private readonly QuotationService _quotationService;
        private readonly ILogger<QuotationController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuotationController"/> class.
        /// </summary>
        public QuotationController(
            QuotationService quotationService,
            ILogger<QuotationController> logger)
        {
            _quotationService = quotationService ?? throw new ArgumentNullException(nameof(quotationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));        }/// <summary>
        /// Creates a new quotation
        /// </summary>
        /// <param name="request">The quotation details to create</param>
        /// <returns>The created quotation</returns>
        /// <response code="201">Returns the newly created quotation</response>
        /// <response code="400">If the request is invalid</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(QuotationResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<QuotationResponseDto>> Create([FromBody] CreateQuotationRequestDto request)
        {
            try
            {                if (!ModelState.IsValid)
                {
                    // Handle JSON deserialization errors differently
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .Where(msg => !msg.Contains("Error converting value {null} to type 'System.Int32'"))
                        .ToList();
                            
                    if (errors.Any())
                    {
                        _logger.LogWarning("Invalid model state when creating quotation: {Errors}", 
                            string.Join(", ", errors));
                            
                        return BadRequest(new
                        {
                            message = "Invalid model state",
                            statusCode = 400,
                            errors = errors
                        });
                    }
                }

                // Validate required fields and data types
                var validationErrors = new List<string>();
                if (string.IsNullOrWhiteSpace(request.QuotationType))
                    validationErrors.Add("QuotationType is required");
                if (string.IsNullOrWhiteSpace(request.OrderType))
                    validationErrors.Add("OrderType is required");
                if (string.IsNullOrWhiteSpace(request.CustomerName))
                    validationErrors.Add("CustomerName is required");
                if (!request.CustomerId.HasValue)
                    validationErrors.Add("CustomerId is required");

                if (validationErrors.Any())
                {
                    _logger.LogWarning("Validation failed when creating quotation: {Errors}", 
                        string.Join(", ", validationErrors));
                        
                    return BadRequest(new
                    {
                        message = "Validation failed",
                        statusCode = 400,
                        errors = validationErrors
                    });
                }

                var result = await _quotationService.CreateQuotationAsync(request);
                _logger.LogInformation("Created new quotation with ID {Id}", result.Id);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid quotation data: {Message}", ex.Message);
                return BadRequest(new
                {
                    message = ex.Message,
                    statusCode = 400
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quotation: {Message}", ex.Message);
                return StatusCode(500, new
                {
                    message = "Failed to create quotation",
                    error = ex.Message,
                    statusCode = 500
                });
            }
        }/// <summary>
        /// Updates an existing quotation
        /// </summary>
        /// <param name="id">The ID of the quotation to update</param>
        /// <param name="request">The updated quotation details</param>
        /// <returns>The updated quotation</returns>        /// <response code="204">If the update was successful</response>
        /// <response code="400">If the request is invalid or ID mismatch</response>
        /// <response code="404">If the quotation was not found</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpPut("{id}")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateQuotationRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state when updating quotation {Id}: {Errors}", 
                        id, string.Join(", ", ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)));
                            
                    return BadRequest(new
                    {
                        message = "Invalid model state",
                        statusCode = 400,
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                    });
                }

                if (id != request.Id)
                {
                    _logger.LogWarning("ID mismatch when updating quotation. URL ID: {UrlId}, Body ID: {BodyId}", 
                        id, request.Id);
                    return BadRequest(new 
                    {
                        message = "ID mismatch between URL and request body",
                        statusCode = 400
                    });
                }

                // Validate required fields not handled by model state
                var validationErrors = new List<string>();
                if (string.IsNullOrWhiteSpace(request.QuotationType))
                    validationErrors.Add("QuotationType is required");
                if (string.IsNullOrWhiteSpace(request.OrderType))
                    validationErrors.Add("OrderType is required");
                if (string.IsNullOrWhiteSpace(request.CustomerName))
                    validationErrors.Add("CustomerName is required");

                if (validationErrors.Any())
                {
                    _logger.LogWarning("Validation failed when updating quotation {Id}: {Errors}", 
                        id, string.Join(", ", validationErrors));
                        
                    return BadRequest(new
                    {
                        message = "Validation failed",
                        statusCode = 400,
                        errors = validationErrors
                    });
                }

                var result = await _quotationService.UpdateQuotationAsync(request);
                if (result == null)
                {
                    _logger.LogWarning("Quotation with ID {Id} not found for update", id);
                    return NotFound(new
                    {
                        message = $"Quotation with ID {id} not found",
                        statusCode = 404
                    });
                }

                _logger.LogInformation("Successfully updated quotation {Id}", id);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid quotation data for update {Id}: {Message}", id, ex.Message);
                return BadRequest(new
                {
                    message = ex.Message,
                    statusCode = 400
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quotation {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new
                {
                    message = "Failed to update quotation",
                    error = ex.Message,
                    statusCode = 500
                });
            }
        }/// <summary>
        /// Get a quotation by ID
        /// </summary>
        /// <param name="id">The ID of the quotation</param>
        /// <returns>The quotation</returns>
        /// <response code="200">Returns the quotation</response>
        /// <response code="404">If the quotation was not found</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(QuotationResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<QuotationResponseDto>> GetById(int id)
        {
            try
            {
                var result = await _quotationService.GetQuotationByIdAsync(id);
                if (result == null)
                {
                    _logger.LogWarning("Quotation with ID {Id} not found", id);
                    return NotFound(new
                    {
                        message = $"Quotation with ID {id} not found",
                        statusCode = 404
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quotation {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new
                {
                    message = "Failed to retrieve quotation",
                    error = ex.Message,
                    statusCode = 500
                });
            }
        }        /// <summary>
        /// Generates a printable HTML version of a quotation
        /// </summary>
        /// <param name="id">The ID of the quotation</param>
        /// <returns>The quotation in HTML format as a downloadable file</returns>
        /// <response code="200">Returns the quotation HTML</response>
        /// <response code="404">If the quotation was not found</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("print/{id}")]
        [Produces("text/html")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Print(int id)
        {
            try
            {
                var htmlContent = await _quotationService.GenerateQuotationHtmlAsync(id);
                if (htmlContent == null)
                {
                    _logger.LogWarning("Quotation with ID {Id} not found for printing", id);
                    return NotFound(new
                    {
                        message = $"Quotation with ID {id} not found",
                        statusCode = 404
                    });
                }

                _logger.LogInformation("Generated printable quotation for ID {Id}", id);
                var fileName = $"Quotation_LD{id:D5}_{DateTime.UtcNow:yyyyMMddHHmmss}.html";
                return File(Encoding.UTF8.GetBytes(htmlContent), "text/html", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating printable quotation {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new
                {
                    message = "Failed to generate printable quotation",
                    error = ex.Message,
                    statusCode = 500
                });
            }
        }
    }
}