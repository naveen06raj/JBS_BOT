using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ERP.API.Models;

namespace ERP.API.Services
{
    public class SalesOrderGridService : ISalesOrderGridService
    {        private readonly string? _connectionString;

        public SalesOrderGridService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(_connectionString))
                throw new ArgumentException("DefaultConnection string is not configured", nameof(configuration));
        }        public async Task<(IEnumerable<SalesOrderGrid> Data, int TotalRecords)> GetSalesOrderGridAsync(SalesOrderGridRequest request)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            
            var parameters = new 
            {
                p_request = System.Text.Json.JsonSerializer.Serialize(new
                {
                    searchText = request.SearchText,
                    customerNames = request.CustomerNames,
                    statuses = request.Statuses,
                    orderIds = request.OrderIds,
                    pageNumber = request.PageNumber,
                    pageSize = request.PageSize,
                    orderBy = request.OrderBy.ToLower(),
                    orderDirection = request.OrderDirection.ToUpper()
                })
            };            var result = await connection.QueryAsync<dynamic>(
                @"SELECT * FROM fn_get_sales_orders_grid(@p_request::json) 
                  AS (total_records INTEGER,
                      id INTEGER,
                      order_id VARCHAR,
                      customer_name VARCHAR,
                      order_date TIMESTAMP WITH TIME ZONE,
                      expected_delivery_date TIMESTAMP WITH TIME ZONE,
                      status VARCHAR,
                      po_id VARCHAR,
                      grand_total NUMERIC(12,2))",
                parameters);

            if (result == null || !result.Any())
                return (new List<SalesOrderGrid>(), 0);            var firstRow = result.First();
            var totalRecords = (int)firstRow.total_records;var gridData = result.Select(row => new SalesOrderGrid
            {
                Id = row.id,
                OrderId = row.order_id,
                CustomerName = row.customer_name,
                OrderDate = row.order_date,
                ExpectedDeliveryDate = row.expected_delivery_date,
                Status = row.status,
                PoId = row.po_id,
                GrandTotal = row.grand_total
            });

            return (gridData, totalRecords);
        }
    }
}
