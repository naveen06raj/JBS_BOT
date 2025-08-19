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

namespace ERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesProductsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SalesProductsController> _logger;
        private readonly string _connectionString;

        public SalesProductsController(IConfiguration configuration, ILogger<SalesProductsController> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionString = _configuration.GetConnectionString("DefaultConnection") ??
                throw new InvalidOperationException("DefaultConnection string is not configured");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalesProducts>>> GetSalesProducts()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(); var result = await connection.QueryAsync<SalesProducts>(
                    @"SELECT 
                        sp.id as Id,
                        sp.user_created as UserCreated,
                        sp.date_created as DateCreated,
                        sp.user_updated as UserUpdated,
                        sp.date_updated as DateUpdated,
                        sp.qty as Quantity,
                        sp.amount as Amount,
                        sp.inventory_items_id as InventoryItemsId,
                        sp.stage as Stage,
                        sp.stage_item_id as StageItemId,
                        sp.isactive as IsActive,
                        m.id as MakeId,
                        COALESCE(m.name, '') as MakeName,
                        md.id as ModelId,
                        COALESCE(md.name, '') as ModelName,
                        p.id as ProductId,
                        COALESCE(p.name, '') as ProductName,
                        c.id as CategoryId,
                        COALESCE(c.name, '') as CategoryName,
                        COALESCE(i.item_code, '') as ItemCode,
                        COALESCE(i.item_name, '') as ItemName
                    FROM sales_products sp
                        LEFT JOIN inventory_items i ON sp.inventory_items_id = i.id
                        LEFT JOIN makes m ON i.make_id = m.id
                        LEFT JOIN models md ON i.model_id = md.id
                        LEFT JOIN products p ON i.product_id = p.id
                        LEFT JOIN categories c ON i.category_id = c.id
                    WHERE sp.isactive = true 
                        AND (i.isactive IS NULL OR i.isactive = true)
                    ORDER BY sp.date_created DESC");

                var resultList = result.ToList();
                return Ok(resultList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving interest products: {Message}", ex.Message);
                return StatusCode(500, new { message = "An error occurred while retrieving interest products", error = ex.Message });
            }
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<SalesProducts>> GetSalesProduct(int id)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();                // First check if the product exists and get its status
                var productStatus = await connection.QueryFirstOrDefaultAsync<(bool exists, bool isActive, bool? inventoryActive)>(
                    @"SELECT 
                        EXISTS(SELECT 1 FROM sales_products WHERE id = @Id) as exists,
                        (SELECT isactive FROM sales_products WHERE id = @Id) as isActive,
                        (SELECT i.isactive FROM sales_products sp 
                         LEFT JOIN inventory_items i ON sp.inventory_items_id = i.id 
                         WHERE sp.id = @Id) as inventoryActive",
                    new { Id = id });

                if (!productStatus.exists)
                {
                    return NotFound($"Sales product with ID {id} not found");
                }

                if (!productStatus.isActive)
                {
                    return NotFound($"Sales product with ID {id} exists but is inactive");
                }                // Get the inventory item ID for more detailed error message
                var inventoryItemId = await connection.QueryFirstOrDefaultAsync<int?>(
                    "SELECT inventory_items_id FROM sales_products WHERE id = @Id",
                    new { Id = id });

                if (productStatus.inventoryActive.HasValue && !productStatus.inventoryActive.Value)
                {
                    return NotFound($"Sales product with ID {id} exists but its inventory item (ID: {inventoryItemId}) is inactive");
                }

                // If all checks pass, get the full product details
                var result = await connection.QueryFirstOrDefaultAsync<SalesProducts>(
                    @"SELECT 
                        sp.id as Id,
                        sp.user_created as UserCreated,
                        sp.date_created as DateCreated,
                        sp.user_updated as UserUpdated,
                        sp.date_updated as DateUpdated,
                        sp.qty as Quantity,
                        sp.amount as Amount,
                        sp.inventory_items_id as InventoryItemsId,
                        sp.stage as Stage,
                        sp.stage_item_id as StageItemId,
                        sp.isactive as IsActive,
                        m.id as MakeId,
                        COALESCE(m.name, '') as MakeName,
                        md.id as ModelId,
                        COALESCE(md.name, '') as ModelName,
                        p.id as ProductId,
                        COALESCE(p.name, '') as ProductName,
                        c.id as CategoryId,
                        COALESCE(c.name, '') as CategoryName,
                        COALESCE(i.item_code, '') as ItemCode,
                        COALESCE(i.item_name, '') as ItemName
                    FROM sales_products sp
                        LEFT JOIN inventory_items i ON sp.inventory_items_id = i.id
                        LEFT JOIN makes m ON i.make_id = m.id
                        LEFT JOIN models md ON i.model_id = md.id
                        LEFT JOIN products p ON i.product_id = p.id
                        LEFT JOIN categories c ON i.category_id = c.id
                    WHERE sp.id = @Id",
                    new { Id = id });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sales product {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new { message = "An error occurred while retrieving the sales product", error = ex.Message });
            }
        }
        [HttpPost]
        public async Task<ActionResult<SalesProducts>> CreateSalesProduct([FromBody] SalesProducts product)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Set default values
                product.IsActive = true;
                product.DateCreated = DateTime.UtcNow;
                var id = await connection.QuerySingleAsync<int>(
                    @"INSERT INTO sales_products 
                    (user_created, date_created, qty, amount, inventory_items_id, stage, stage_item_id, unit_price, isactive) 
                    VALUES 
                    (@UserCreated, @DateCreated, @Quantity, @Amount, @InventoryItemsId, @Stage, @StageItemId, @UnitPrice, @IsActive)
                    RETURNING id",
                    new
                    {
                        product.UserCreated,
                        product.DateCreated,
                        product.Quantity,
                        product.Amount,
                        product.InventoryItemsId,
                        product.Stage,
                        product.StageItemId,
                        product.UnitPrice,
                        product.IsActive
                    });

                return CreatedAtAction(nameof(GetSalesProduct), new { id }, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sales product: {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred while creating the sales product");
            }
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSalesProduct(int id, [FromBody] SalesProducts product)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (id != product.Id)
                {
                    return BadRequest("ID mismatch");
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Set update values
                product.DateUpdated = DateTime.UtcNow;

                var result = await connection.ExecuteAsync(
                    @"UPDATE sales_products SET
                    user_updated = @UserUpdated,
                    date_updated = @DateUpdated,
                    qty = @Quantity,
                    amount = @Amount,
                    inventory_items_id = @InventoryItemsId,
                    stage = @Stage,
                    stage_item_id = @StageItemId
                    WHERE id = @Id",
                    new
                    {
                        product.Id,
                        product.UserUpdated,
                        product.DateUpdated,
                        product.Quantity,
                        product.Amount,
                        product.InventoryItemsId,
                        product.Stage,
                        product.StageItemId
                    });

                if (result == 0)
                {
                    return NotFound($"Sales product with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sales product {Id}: {Message}", id, ex.Message);
                return StatusCode(500, "An unexpected error occurred while updating the sales product");
            }
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSalesProduct(int id)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                await connection.ExecuteAsync(
                    "CALL sp_delete_sales_product(@Id)",
                    new { Id = id });

                return Ok(new { message = $"Interest product {id} deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting interest product {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new { message = "An error occurred while deleting the interest product", error = ex.Message });
            }
        }

        [HttpGet("stage/{stage}/{stageItemId}")]
        public async Task<ActionResult<IEnumerable<SalesProducts>>> GetByStage(string stage, long stageItemId)
        {
            try
            {
                if (string.IsNullOrEmpty(stage))
                {
                    return BadRequest("Stage is required");
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"SELECT 
                        sp.id as Id,
                        sp.user_created as UserCreated,
                        sp.date_created as DateCreated,
                        sp.user_updated as UserUpdated,
                        sp.date_updated as DateUpdated,
                        sp.qty as Quantity,
                        sp.amount as Amount,
                        sp.inventory_items_id as InventoryItemsId,
                        sp.stage as Stage,
                        sp.stage_item_id as StageItemId,
                        sp.isactive as IsActive,
                        m.id as MakeId,
                        COALESCE(m.name, '') as MakeName,
                        md.id as ModelId,
                        COALESCE(md.name, '') as ModelName,
                        p.id as ProductId,
                        COALESCE(p.name, '') as ProductName,
                        c.id as CategoryId,
                        COALESCE(c.name, '') as CategoryName,
                        COALESCE(i.item_code, '') as ItemCode,
                        COALESCE(i.item_name, '') as ItemName
                    FROM sales_products sp
                        LEFT JOIN inventory_items i ON sp.inventory_items_id = i.id
                        LEFT JOIN makes m ON i.make_id = m.id
                        LEFT JOIN models md ON i.model_id = md.id
                        LEFT JOIN products p ON i.product_id = p.id
                        LEFT JOIN categories c ON i.category_id = c.id
                    WHERE sp.stage = @Stage 
                        AND sp.stage_item_id = @StageItemId 
                        AND sp.isactive = true 
                        AND (i.isactive IS NULL OR i.isactive = true)
                    ORDER BY sp.date_created DESC";

                var products = await connection.QueryAsync<SalesProducts>(query, new { Stage = stage, StageItemId = stageItemId });

                if (!products.Any())
                {
                    return NotFound($"No products found for stage {stage} and item ID {stageItemId}");
                }

                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products for stage {Stage} and item ID {StageItemId}: {Message}",
                    stage, stageItemId, ex.Message);
                return StatusCode(500, new { message = "An error occurred while retrieving the products", error = ex.Message });
            }
        }

        [HttpPut("{id}/activate")]
        public async Task<IActionResult> ActivateProduct(int id, [FromQuery] int userId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // First check if the product exists
                var product = await connection.QueryFirstOrDefaultAsync<(int? id, int? inventoryItemId)>(
                    "SELECT id, inventory_items_id FROM sales_products WHERE id = @Id",
                    new { Id = id });

                if (!product.id.HasValue)
                {
                    return NotFound($"Sales product with ID {id} not found");
                }

                // Begin transaction since we're updating multiple tables
                using var transaction = connection.BeginTransaction();
                try
                {
                    // Update sales product
                    await connection.ExecuteAsync(
                        @"UPDATE sales_products 
                        SET isactive = true,
                            user_updated = @UserId,
                            date_updated = CURRENT_TIMESTAMP
                        WHERE id = @Id",
                        new { Id = id, UserId = userId });

                    // If there's a linked inventory item, activate it too
                    if (product.inventoryItemId.HasValue)
                    {
                        await connection.ExecuteAsync(
                            @"UPDATE inventory_items 
                            SET isactive = true,
                                updated_by = @UserId,
                                updated_date = CURRENT_TIMESTAMP
                            WHERE id = @Id",
                            new { Id = product.inventoryItemId, UserId = userId });
                    }

                    transaction.Commit();
                    return Ok(new { message = $"Successfully activated sales product {id} and its inventory item" });
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating sales product {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new { message = "An error occurred while activating the sales product", error = ex.Message });
            }
        }
    }
}
