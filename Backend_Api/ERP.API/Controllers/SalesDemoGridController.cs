using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ERP.API.Models;
using ERP.API.Services;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesDemoGridController : ControllerBase
    {
        private readonly ISalesDemoGridService _salesDemoGridService;

        public SalesDemoGridController(ISalesDemoGridService salesDemoGridService)
        {
            _salesDemoGridService = salesDemoGridService;
        }        [HttpPost("search")]
        public async Task<IActionResult> SearchSalesDemoGrid([FromBody] SalesDemoGridRequest request)
        {
            try 
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Invalid request parameters",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                    });
                }

                var (data, totalRecords) = await _salesDemoGridService.GetSalesDemoGridAsync(request);
                var response = new SalesDemoGridResponse<SalesDemoGrid>
                {
                    TotalRecords = totalRecords,
                    Data = data
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    message = "An error occurred while processing your request",
                    error = ex.Message
                });
            }
        }
    }
}
