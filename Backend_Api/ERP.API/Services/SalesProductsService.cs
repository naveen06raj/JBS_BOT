using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using ERP.API.Models;
using Dapper;
using Npgsql;

namespace ERP.API.Services
{    public class SalesProductsService : BaseDataService<SalesProducts>
    {
        public SalesProductsService(string connectionString) 
            : base(connectionString, "sales_products")
        {
        }        public override async Task<IEnumerable<SalesProducts>> GetAllAsync(string? whereClause = "", object? parameters = null)
        {
            using var connection = CreateConnection();
            var query = @"
                SELECT sp.*, 
                       COALESCE(m.name, '') as make,
                       COALESCE(md.name, '') as model,
                       COALESCE(p.name, '') as product,
                       COALESCE(c.name, '') as category,
                       COALESCE(i.item_code, '') as item_code,
                       COALESCE(i.item_name, '') as item_name                FROM sales_products sp 
                LEFT JOIN inventory_items i ON sp.inventory_items_id = i.id
                LEFT JOIN makes m ON i.make_id = m.id
                LEFT JOIN models md ON i.model_id = md.id
                LEFT JOIN products p ON i.product_id = p.id
                LEFT JOIN categories c ON i.category_id = c.id
                WHERE (i.isactive = true OR i.isactive IS NULL)";
            
            if (!string.IsNullOrEmpty(whereClause))
            {
                query += $" WHERE {whereClause}";
            }
              var results = await connection.QueryAsync<SalesProducts>(query, parameters);
            return results ?? Enumerable.Empty<SalesProducts>();
        }        public override async Task<SalesProducts?> GetByIdAsync(int? id)
        {
            if (!id.HasValue)
                return null;
                
            using var connection = CreateConnection();
            var query = @"SELECT sp.*, 
                       COALESCE(m.name, '') as make,
                       COALESCE(md.name, '') as model,
                       COALESCE(p.name, '') as product,
                       COALESCE(c.name, '') as category,
                       COALESCE(i.item_code, '') as item_code,
                       COALESCE(i.item_name, '') as item_name                FROM sales_products sp 
                LEFT JOIN inventory_items i ON sp.inventory_items_id = i.id
                LEFT JOIN makes m ON i.make_id = m.id
                LEFT JOIN models md ON i.model_id = md.id
                LEFT JOIN products p ON i.product_id = p.id
                LEFT JOIN categories c ON i.category_id = c.id
                WHERE sp.id = @Id AND (i.isactive = true OR i.isactive IS NULL)";
              return await connection.QueryFirstOrDefaultAsync<SalesProducts>(query, new { Id = id });
        }

        protected override string GenerateInsertQuery()
          {
            var properties = typeof(SalesProducts).GetProperties()
                .Where(p => p.Name.ToLower() != "id");
            
            var columns = string.Join(", ", properties.Select(p => GetColumnName(p)));
            var parameters = string.Join(", ", properties.Select(p => $"@{p.Name}"));
            
            return $"INSERT INTO {_tableName} ({columns}) VALUES ({parameters}) RETURNING id";
        }

        protected override string GenerateUpdateQuery()
        {            var properties = typeof(SalesProducts).GetProperties()
                .Where(p => p.Name.ToLower() != "id" && 
                           p.Name.ToLower() != "dateupdated");
            
            var setClauses = string.Join(", ", properties.Select(p => $"{GetColumnName(p)} = @{p.Name}"));
            
            return $"UPDATE {_tableName} SET {setClauses}, date_updated = CURRENT_TIMESTAMP WHERE id = @Id";
        }
    }
}