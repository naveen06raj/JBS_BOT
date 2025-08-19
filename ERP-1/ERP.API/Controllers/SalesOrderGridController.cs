using Microsoft.AspNetCore.Mvc;
using ERP.API.Models;
using ERP.API.Services;
using System.Threading.Tasks;

namespace ERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesOrderGridController : ControllerBase
    {
        private readonly ISalesOrderGridService _salesOrderGridService;

        public SalesOrderGridController(ISalesOrderGridService salesOrderGridService)
        {
            _salesOrderGridService = salesOrderGridService;
        }        [HttpPost("search")]
        public async Task<IActionResult> SearchSalesOrderGrid([FromBody] SalesOrderGridRequest request)
        {
            var (data, totalRecords) = await _salesOrderGridService.GetSalesOrderGridAsync(request);

            var response = new SalesOrderGridResponse
            {
                TotalRecords = totalRecords,
                Data = data
            };

            return Ok(response);
        }
    }
}
