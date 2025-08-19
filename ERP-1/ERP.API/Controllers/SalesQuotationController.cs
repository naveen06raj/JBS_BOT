using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ERP.API.Models;
using ERP.API.Models.DTOs;
using ERP.API.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ERP.API.Controllers
{    [ApiController]
    [Route("api/sales-quotations")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public class SalesQuotationController : ControllerBase
    {
        private readonly SalesQuotationService _salesQuotationService;
        private readonly SalesSummaryService _summaryService;
        private readonly ILogger<SalesQuotationController> _logger;

        public SalesQuotationController(
            SalesQuotationService salesQuotationService,
            SalesSummaryService summaryService,
            ILogger<SalesQuotationController> logger)
        {
            _salesQuotationService = salesQuotationService;
            _summaryService = summaryService;
            _logger = logger;
        }

        /// <summary>
        /// Get all quotations
        /// </summary>
        /// <returns>List of all quotations</returns>        /// <response code="200">Returns the list of quotations</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<QuotationResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<QuotationResponseDto>>> GetAll()
        {
            try
            {
                var quotations = await _salesQuotationService.GetAllAsync();
                return Ok(quotations.Select(q => MapToResponseDto(q)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all quotations");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get quotation by ID
        /// </summary>
        /// <param name="id">The ID of the quotation to retrieve</param>
        /// <returns>The requested quotation</returns>
        /// <response code="200">Returns the requested quotation</response>
        /// <response code="404">If the quotation is not found</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(QuotationResponseDto), StatusCodes.Status200OK)]
                [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<QuotationResponseDto>> GetById(int id)
        {
            try
            {
                var quotation = await _salesQuotationService.GetByIdAsync(id);
                if (quotation == null)
                    return NotFound(new { error = $"Quotation with ID {id} not found" });

                return Ok(MapToResponseDto(quotation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quotation {Id}", id);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete quotation
        /// </summary>
        /// <param name="id">The ID of the quotation to delete</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">If the quotation was successfully deleted</response>
        /// <response code="404">If the quotation is not found</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var quotation = await _salesQuotationService.GetByIdAsync(id);
                if (quotation == null)
                    return NotFound(new { error = $"Quotation with ID {id} not found" });

                var success = await _salesQuotationService.DeleteAsync(id);
                if (!success)
                    return StatusCode(500, new { error = "Failed to delete quotation" });                var summary = new SalesSummary 
                {                    Title = "Quotation deleted",
                    Description = $"Quotation {quotation.QuotationId ?? Convert.ToString(id)} deleted for {quotation.CustomerName ?? string.Empty}",
                    DateTime = DateTime.UtcNow,
                    Stage = "quotation",
                    StageItemId = Convert.ToString(id),
                    IsActive = true,
                    UserCreated = 1, // Default system user since auth is removed
                    Entities = JsonSerializer.Serialize(new { QuotationId = quotation.QuotationId ?? Convert.ToString(id), CustomerName = quotation.CustomerName ?? string.Empty })
                };
                await _summaryService.CreateAsync(summary);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting quotation {Id}", id);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get quotations by opportunity ID
        /// </summary>
        /// <param name="opportunityId">The ID of the opportunity</param>
        /// <returns>List of quotations for the opportunity</returns>
       
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("opportunity/{opportunityId}")]
        [ProducesResponseType(typeof(IEnumerable<QuotationResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<QuotationResponseDto>>> GetByOpportunityId(string opportunityId)
        {
            try
            {
                var quotations = await _salesQuotationService.GetQuotationsByOpportunityIdAsync(opportunityId);
                return Ok(quotations.Select(q => MapToResponseDto(q)));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid opportunity ID format {OpportunityId}", opportunityId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quotations for opportunity {OpportunityId}", opportunityId);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get quotations by customer ID
        /// </summary>
        /// <param name="customerId">The ID of the customer</param>
        /// <returns>List of quotations for the customer</returns>
        /// <response code="200">Returns the list of quotations</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("customer/{customerId}")]
        [ProducesResponseType(typeof(IEnumerable<QuotationResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<QuotationResponseDto>>> GetByCustomerId(string customerId)
        {
            try
            {
                var quotations = await _salesQuotationService.GetQuotationsByCustomerIdAsync(customerId);
                return Ok(quotations.Select(q => MapToResponseDto(q)));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid customer ID format {CustomerId}", customerId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quotations for customer {CustomerId}", customerId);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }        private static QuotationResponseDto MapToResponseDto(SalesQuotation quotation)
        {
            if (quotation == null)
                throw new ArgumentNullException(nameof(quotation));

            return new QuotationResponseDto
            {
                Id = quotation.Id.GetValueOrDefault(),
                UserCreated = quotation.UserCreated.GetValueOrDefault(),
                DateCreated = quotation.DateCreated.GetValueOrDefault(DateTime.UtcNow),
                UserUpdated = quotation.UserUpdated,
                DateUpdated = quotation.DateUpdated,
                Version = quotation.Version ?? "1.0",
                Terms = quotation.Terms ?? string.Empty,
                ValidTill = quotation.ValidTill.GetValueOrDefault(DateTime.UtcNow.AddDays(30)),
                QuotationFor = quotation.QuotationFor ?? string.Empty,
                Status = quotation.Status ?? "Draft",
                LostReason = quotation.LostReason,
                CustomerId = quotation.CustomerId,
                CustomerName = quotation.CustomerName ?? string.Empty,
                QuotationType = quotation.QuotationType ?? string.Empty,
                QuotationDate = quotation.QuotationDate.GetValueOrDefault(DateTime.UtcNow),
                OrderType = quotation.OrderType ?? string.Empty,
                Comments = quotation.Comments ?? string.Empty,
                DeliveryWithin = quotation.DeliveryWithin,
                DeliveryAfter = quotation.DeliveryPrepareAfter,
                IsActive = quotation.IsActive,
                QuotationId = quotation.QuotationId,
                OpportunityId = quotation.OpportunityId,
                LeadId = quotation.LeadId,
                Taxes = quotation.Taxes ?? string.Empty,
                Delivery = quotation.Delivery ?? string.Empty,
                Payment = quotation.Payment ?? string.Empty,
                Warranty = quotation.Warranty ?? string.Empty,
                FreightCharge = quotation.FreightCharge,
                IsCurrent = quotation.IsCurrent.GetValueOrDefault(),
                ParentSalesQuotationsId = quotation.ParentSalesQuotationsId
            };
        }

        /// <summary>
        /// Create a new quotation
        /// </summary>
        /// <param name="request">The quotation details to create</param>
        /// <returns>The ID of the created quotation</returns>
        /// <response code="201">Returns the newly created quotation ID</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<int>> Create([FromBody] CreateQuotationRequestDto request)
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

                var quotation = new SalesQuotation
                {
                    UserCreated = request.UserCreated ?? 1, // Default system user since auth is removed
                    DateCreated = DateTime.UtcNow,
                    IsActive = true,  // This is always true for new quotations                    Status = request.Status ?? "Draft",
                    IsCurrent = true, // This is always true for new quotations
                    Version = request.Version ?? "1.0", // Default version for new quotations
                    Terms = request.Terms,ValidTill = request.ValidTill ?? DateTime.UtcNow.AddDays(30),
                    QuotationFor = request.QuotationFor,
                    LostReason = request.LostReason,                    CustomerId = request.CustomerId ?? throw new ArgumentException("CustomerId is required"),                    QuotationType = request.QuotationType,
                    QuotationDate = request.QuotationDate,
                    OrderType = request.OrderType,
                    Comments = request.Comments,
                    DeliveryWithin = request.DeliveryWithin,
                    DeliveryAfter = request.DeliveryAfter,
                    QuotationId = request.QuotationId,
                    OpportunityId = request.OpportunityId ?? 0,
                    LeadId = request.LeadId ?? 0,
                    CustomerName = request.CustomerName,
                    Taxes = request.Taxes,
                    Delivery = request.Delivery,
                    Payment = request.Payment,
                    Warranty = request.Warranty,
                    FreightCharge = request.FreightCharge,
                    ParentSalesQuotationsId = request.ParentSalesQuotationsId
                };

                var id = await _salesQuotationService.CreateAsync(quotation);
                _logger.LogInformation("Created new quotation with ID {Id}", id);                // Get the created quotation with all its data
                var createdQuotation = await _salesQuotationService.GetByIdAsync(id);
                if (createdQuotation == null)
                {
                    throw new Exception($"Failed to retrieve created quotation with ID {id}");
                }
                var responseDto = MapToResponseDto(createdQuotation);

                // Create summary entry
                var summary = new SalesSummary
                {
                    Title = "New Quotation Created",                    Description = $"New quotation created for {quotation.CustomerName ?? "Unknown"}",
                    DateTime = DateTime.UtcNow,
                    Stage = "quotation",
                    StageItemId = Convert.ToString(id),
                    IsActive = true,
                    UserCreated = 1, // Default system user since auth is removed
                    Entities = JsonSerializer.Serialize(new { QuotationId = id.ToString(), CustomerName = quotation.CustomerName ?? "Unknown" })
                };
                await _summaryService.CreateAsync(summary);

                // Return 201 Created with just the ID
                return CreatedAtAction(nameof(GetById), new { id }, id);
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
        }

        /// <summary>
        /// Update an existing quotation
        /// </summary>
        /// <param name="id">The ID of the quotation to update</param>
        /// <param name="request">The updated quotation data</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">If the quotation was successfully updated</response>
        /// <response code="400">If the quotation data is invalid or IDs don't match</response>
        /// <response code="404">If the quotation is not found</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpPut("{id:int}")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(QuotationResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateQuotationRequestDto request)
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

                if (id != request.Id)
                {
                    return BadRequest(new { 
                        message = "ID mismatch between URL and request body",
                        statusCode = 400 
                    });
                }

                if (request.CustomerId == null)
                {
                    return BadRequest(new {
                        message = "CustomerId is required",
                        statusCode = 400
                    });
                }

                var existingQuotation = await _salesQuotationService.GetByIdAsync(id);
                if (existingQuotation == null)
                    return NotFound(new { 
                        message = $"Quotation with ID {id} not found",
                        statusCode = 404 
                    });

                // Update existing quotation with request data
                existingQuotation.Id = request.Id;
                existingQuotation.UserUpdated = 1; // Default to system user since auth is removed
                existingQuotation.DateUpdated = DateTime.UtcNow;
                existingQuotation.Version = request.Version ?? existingQuotation.Version;
                existingQuotation.Terms = request.Terms ?? existingQuotation.Terms;
                existingQuotation.ValidTill = request.ValidTill;
                existingQuotation.QuotationFor = request.QuotationFor ?? existingQuotation.QuotationFor;
                existingQuotation.Status = request.Status ?? existingQuotation.Status;
                existingQuotation.LostReason = request.LostReason ?? existingQuotation.LostReason;
                existingQuotation.CustomerId = Convert.ToInt32(request.CustomerId);
                existingQuotation.QuotationType = request.QuotationType ?? existingQuotation.QuotationType;
                existingQuotation.QuotationDate = request.QuotationDate;
                existingQuotation.OrderType = request.OrderType ?? existingQuotation.OrderType;
                existingQuotation.Comments = request.Comments ?? existingQuotation.Comments;
                existingQuotation.DeliveryWithin = request.DeliveryWithin ?? existingQuotation.DeliveryWithin;
                existingQuotation.DeliveryAfter = request.DeliveryAfter ?? existingQuotation.DeliveryAfter;
                existingQuotation.QuotationId = request.QuotationId;
                existingQuotation.OpportunityId = Convert.ToInt32(request.OpportunityId ?? 0);
                existingQuotation.LeadId = Convert.ToInt32(request.LeadId ?? 0);
                existingQuotation.CustomerName = request.CustomerName ?? existingQuotation.CustomerName;
                existingQuotation.Taxes = request.Taxes ?? existingQuotation.Taxes;
                existingQuotation.Delivery = request.Delivery ?? existingQuotation.Delivery;
                existingQuotation.Payment = request.Payment ?? existingQuotation.Payment;
                existingQuotation.Warranty = request.Warranty ?? existingQuotation.Warranty;
                existingQuotation.FreightCharge = request.FreightCharge ?? existingQuotation.FreightCharge;
                existingQuotation.ParentSalesQuotationsId = request.ParentSalesQuotationsId;

                // Update the quotation and get the update status
                var updateSuccess = await _salesQuotationService.UpdateAsync(existingQuotation);
                if (!updateSuccess)
                {
                    return StatusCode(500, new { 
                        message = "Failed to update quotation",
                        statusCode = 500 
                    });
                }

                // Create summary entry
                var summary = new SalesSummary
                {
                    Title = "Quotation Updated",
                    Description = $"Quotation {existingQuotation.QuotationId ?? Convert.ToString(id)} updated for {existingQuotation.CustomerName ?? "Unknown"}",
                    DateTime = DateTime.UtcNow,
                    Stage = "quotation",
                    StageItemId = Convert.ToString(id),
                    IsActive = true,
                    UserCreated = 1, // Default system user since auth is removed
                    Entities = JsonSerializer.Serialize(new { QuotationId = id.ToString(), CustomerName = existingQuotation.CustomerName ?? "Unknown" })
                };
                await _summaryService.CreateAsync(summary);

                // Get and return the updated quotation
                var updatedQuotation = await _salesQuotationService.GetByIdAsync(id);
                if (updatedQuotation == null)
                {
                    return StatusCode(500, new { 
                        message = "Failed to retrieve updated quotation",
                        statusCode = 500 
                    });
                }
                return Ok(MapToResponseDto(updatedQuotation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quotation {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new { 
                    message = "Failed to update quotation",
                    error = ex.Message,
                    statusCode = 500 
                });
            }
        }

        /// <summary>
        /// Get quotations by lead ID
        /// </summary>
        /// <param name="leadId">The ID of the lead</param>
        /// <returns>List of quotations for the lead</returns>
        /// <response code="200">Returns the list of quotations</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("lead/{leadId}")]
        [ProducesResponseType(typeof(IEnumerable<QuotationResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<QuotationResponseDto>>> GetByLeadId(string leadId)
        {
            try
            {
                var quotations = await _salesQuotationService.GetQuotationsByLeadIdAsync(leadId);
                return Ok(quotations.Select(q => MapToResponseDto(q)));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid lead ID format {LeadId}", leadId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quotations for lead {LeadId}", leadId);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }
    }
}
