using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ERP.API.Models;
using ERP.API.Models.DTOs;
using ERP.API.Services;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;

namespace ERP.API.Controllers
{    [ApiController]
    [Route("api/[controller]")]
    public class SalesOrderController : ControllerBase
    {
        private readonly ISalesOrderService _salesOrderService;

        public SalesOrderController(ISalesOrderService salesOrderService)
        {
            _salesOrderService = salesOrderService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalesOrderGrid>>> GetAll()
        {
            var result = await _salesOrderService.GetAllSalesOrdersAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SalesOrder>> GetById(int id)
        {
            var salesOrder = await _salesOrderService.GetSalesOrderByIdAsync(id);
            if (salesOrder == null)
            {
                return NotFound();
            }
            return Ok(salesOrder);
        }        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] SalesOrder salesOrder)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }            var id = await _salesOrderService.CreateSalesOrderAsync(salesOrder);
            return Ok(id);
        }        [HttpPut("{id}")]
        public async Task<ActionResult<SalesOrder>> Update(int id, [FromBody] SalesOrder salesOrder)
        {
            if (id != salesOrder.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedSalesOrder = await _salesOrderService.UpdateSalesOrderAsync(salesOrder);
            if (updatedSalesOrder == null)
            {
                return NotFound();
            }

            return Ok(updatedSalesOrder);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _salesOrderService.DeleteSalesOrderAsync(id);
            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }        [HttpGet("quotation/{id}")]
        public async Task<ActionResult<QuotationWithOrderResponse>> GetQuotationById(int id)
        {
            try
            {
                var quotation = await _salesOrderService.GetQuotationByIdAsync(id);
                if (quotation == null)
                {
                    return NotFound(new
                    {
                        message = $"Quotation with ID {id} not found",
                        statusCode = 404
                    });
                }
                return Ok(quotation);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to retrieve quotation",
                    error = ex.Message,
                    statusCode = 500
                });
            }
        }
    }
}
