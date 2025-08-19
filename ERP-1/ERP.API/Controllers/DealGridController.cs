using System.Threading.Tasks;
using ERP.API.Models.DealGrid;
using ERP.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DealGridController : ControllerBase
    {
        private readonly ISalesDealGridService _dealGridService;

        public DealGridController(ISalesDealGridService dealGridService)
        {
            _dealGridService = dealGridService;
        }        [HttpPost("GetDealsGrid")]
        public async Task<ActionResult<DealGridPaginatedResponse>> GetDealsGrid([FromBody] DealGridRequest request)
        {
            try
            {
                var result = await _dealGridService.GetDealsGridAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
