using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ERP.API.Models;

namespace ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesItemsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SalesItemsController> _logger;
        private readonly string _connectionString;

        public SalesItemsController(IConfiguration configuration, ILogger<SalesItemsController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet("{itemId}")]
        public async Task<IActionResult> GetSalesItem(int itemId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                var sql = "SELECT * FROM get_sales_item_details(@ItemId)";
                var item = await connection.QueryFirstOrDefaultAsync(sql, new { ItemId = itemId });
                if (item == null)
                {
                    return NotFound(new { message = $"Item with id {itemId} not found." });
                }
                return Ok(item);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error fetching sales item: {Message}", ex.Message);
                return StatusCode(500, new { message = "An error occurred while fetching the sales item.", error = ex.Message });
            }
        }
    }
}
