using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using ERP.API.Models;
using ERP.API.Services;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DealController : ControllerBase
    {
        private readonly SalesDealService _dealService;
        private readonly SalesSummaryService _summaryService;

        public DealController(SalesDealService dealService, SalesSummaryService summaryService)
        {
            _dealService = dealService;
            _summaryService = summaryService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalesDeal>>> GetAll()
        {
            var deals = await _dealService.GetAllAsync();
            return Ok(deals);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SalesDeal>> GetById(int id)
        {
            var deal = await _dealService.GetByIdAsync(id);
            if (deal == null)
                return NotFound($"Deal with ID {id} not found");

            return Ok(deal);
        }

        [HttpGet("opportunity/{opportunityId}")]
        public async Task<ActionResult<IEnumerable<SalesDeal>>> GetByOpportunityId(int opportunityId)
        {
            var deals = await _dealService.GetAllAsync($"opportunity_id = @OpportunityId", new { OpportunityId = opportunityId });
            if (!deals.Any())
            {
                return NotFound($"No deals found for opportunity ID {opportunityId}");
            }

            return Ok(deals);
        }

        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<int>> Create([FromBody] SalesDeal deal)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Invalid model state",
                        statusCode = 400,
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                    });
                }

                // Validate required fields
                var validationErrors = new List<string>();
                if (string.IsNullOrWhiteSpace(deal.DealName))
                {
                    validationErrors.Add("Deal Name is required");
                }

                if (validationErrors.Any())
                {
                    return BadRequest(new
                    {
                        message = "Validation failed",
                        statusCode = 400,
                        errors = validationErrors
                    });
                }

                // Set default values
                deal.IsActive = deal.IsActive ?? true;
                deal.DateCreated = DateTime.UtcNow;
                deal.DateUpdated = DateTime.UtcNow;
                deal.Status = string.IsNullOrEmpty(deal.Status) ? "New" : deal.Status;

                // Create the deal
                var id = await _dealService.CreateAsync(deal);

                // Create summary entry
                var summary = new SalesSummary
                {
                    Title = "Deal created",
                    Description = $"New deal created: {deal.DealName}",
                    DateTime = DateTime.UtcNow,
                    Stage = "deal",
                    StageItemId = id.ToString(),
                    IsActive = true,
                    Entities = System.Text.Json.JsonSerializer.Serialize(new { DealId = id, DealName = deal.DealName })
                };
                await _summaryService.CreateAsync(summary);

                return CreatedAtAction(nameof(GetById), new { id }, id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while creating the deal",
                    statusCode = 500,
                    errors = new[] { ex.Message }
                });
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] SalesDeal deal)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Invalid model state",
                        statusCode = 400,
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                    });
                }

                var existingDeal = await _dealService.GetByIdAsync(id);
                if (existingDeal == null)
                {
                    return NotFound(new
                    {
                        message = $"Deal with ID {id} not found",
                        statusCode = 404,
                        errors = new[] { $"Deal with ID {id} not found" }
                    });
                }

                // Update fields
                existingDeal.DealName = deal.DealName?.Trim() ?? existingDeal.DealName;
                existingDeal.Amount = deal.Amount ?? existingDeal.Amount;
                existingDeal.ExpectedRevenue = deal.ExpectedRevenue ?? existingDeal.ExpectedRevenue;
                existingDeal.StartDate = deal.StartDate ?? existingDeal.StartDate;
                existingDeal.DealFor = deal.DealFor?.Trim() ?? existingDeal.DealFor;
                existingDeal.CloseDate = deal.CloseDate ?? existingDeal.CloseDate;
                existingDeal.Status = deal.Status?.Trim() ?? existingDeal.Status;
                existingDeal.IsActive = deal.IsActive ?? existingDeal.IsActive;
                existingDeal.Comments = deal.Comments?.Trim() ?? existingDeal.Comments;
                existingDeal.OpportunityId = deal.OpportunityId ?? existingDeal.OpportunityId;
                existingDeal.CustomerId = deal.CustomerId ?? existingDeal.CustomerId;
                existingDeal.SalesRepresentativeId = deal.SalesRepresentativeId ?? existingDeal.SalesRepresentativeId;
                existingDeal.UserUpdated = deal.UserUpdated;
                existingDeal.DateUpdated = DateTime.UtcNow;

                var success = await _dealService.UpdateAsync(existingDeal);
                if (!success)
                {
                    return StatusCode(500, new
                    {
                        message = $"Failed to update deal {id}",
                        statusCode = 500,
                        errors = new[] { "Database update operation failed" }
                    });
                }

                // Create summary entry for update
                var summary = new SalesSummary
                {
                    Title = $"Deal updated",
                    Description = $"Deal information updated for {existingDeal.DealName}",
                    DateTime = DateTime.UtcNow,
                    Stage = "deal",
                    StageItemId = id.ToString(),
                    IsActive = true,
                    Entities = System.Text.Json.JsonSerializer.Serialize(new { DealId = id, DealName = existingDeal.DealName })
                };
                await _summaryService.CreateAsync(summary);

                return Ok(existingDeal);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Failed to update deal {id}",
                    statusCode = 500,
                    errors = new[] { ex.Message }
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deal = await _dealService.GetByIdAsync(id);
            if (deal == null)
                return NotFound($"Deal with ID {id} not found");

            await _dealService.DeleteAsync(id);

            // Create summary entry for deletion
            var summary = new SalesSummary
            {
                Title = $"Deal deleted - {deal.DealName}",
                Description = $"Deal deleted: {deal.DealName}",
                DateTime = DateTime.UtcNow,
                Stage = "deal",
                StageItemId = id.ToString(),
                IsActive = true,
                Entities = System.Text.Json.JsonSerializer.Serialize(new { DealId = id, DealName = deal.DealName })
            };
            await _summaryService.CreateAsync(summary);

            return NoContent();
        }

        [HttpGet("details/{id}")]
        public async Task<ActionResult<SalesDealDetails>> GetDealDetailsById(int id)
        {
            var dealDetails = await _dealService.GetDealDetailsByIdAsync(id);
            if (dealDetails == null)
                return NotFound($"Deal details with ID {id} not found");

            return Ok(dealDetails);
        }
    }
}
