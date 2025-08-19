using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using Microsoft.Extensions.Logging;
using System.Linq;
using ERP.API.Models;
using ERP.API.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductDropdownController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProductDropdownController> _logger;
        private readonly string _connectionString;

        public ProductDropdownController(IConfiguration configuration, ILogger<ProductDropdownController> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionString = _configuration.GetConnectionString("DefaultConnection") ?? 
                throw new InvalidOperationException("DefaultConnection string is not configured");
        }

        [HttpGet("options")]
        public async Task<ActionResult<IEnumerable<ProductDropdownOptions>>> GetDropdownOptions()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var result = await connection.QueryAsync<ProductDropdownOptions>(
                    @"SELECT * FROM get_product_dropdown_options()");

                var resultList = result.ToList();

                if (!resultList.Any())
                {
                    _logger.LogWarning("No product dropdown options found in database");
                }

                return Ok(resultList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dropdown options: {Message}", ex.Message);
                return StatusCode(500, new { message = "An error occurred while retrieving dropdown options", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a simplified list of products with only item code and name
        /// </summary>
        /// <returns>List of products with item code and name</returns>
        /// <response code="200">Returns the list of products</response>
        /// <response code="500">If there was an error retrieving the products</response>
        [HttpGet("product-list")]
        [ProducesResponseType(typeof(IEnumerable<ProductListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ProductListDto>>> GetProductList()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();                var result = await connection.QueryAsync<ProductListDto>(
                    @"SELECT id as InventoryItemId, item_code as ItemCode, item_name as ItemName 
                      FROM inventory_items 
                      WHERE item_code IS NOT NULL AND item_name IS NOT NULL 
                      ORDER BY item_name");

                var resultList = result.ToList();

                if (!resultList.Any())
                {
                    _logger.LogWarning("No products found in database");
                }

                return Ok(resultList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product list: {Message}", ex.Message);
                return StatusCode(500, new { message = "An error occurred while retrieving product list", error = ex.Message });
            }
        }
    }
}