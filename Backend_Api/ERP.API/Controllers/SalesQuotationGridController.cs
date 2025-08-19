using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ERP.API.Models;
using ERP.API.Services;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesQuotationGridController : ControllerBase
    {
        private readonly ISalesQuotationGridService _salesQuotationGridService;

        public SalesQuotationGridController(ISalesQuotationGridService salesQuotationGridService)
        {
            _salesQuotationGridService = salesQuotationGridService;
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchSalesQuotationGrid([FromBody] SalesQuotationGridRequest request)
        {
            var (data, totalRecords) = await _salesQuotationGridService.GetSalesQuotationGridAsync(request);

            var response = new SalesQuotationGridResponse
            {
                TotalRecords = totalRecords,
                Data = data
            };

            return Ok(response);
        }
    }
}
