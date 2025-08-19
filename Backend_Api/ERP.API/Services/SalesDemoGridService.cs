using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ERP.API.Models;
using System.Text.Json;

namespace ERP.API.Services
{
    public interface ISalesDemoGridService
    {
        Task<(IEnumerable<SalesDemoGrid> Data, int TotalRecords)> GetSalesDemoGridAsync(SalesDemoGridRequest request);
    }

    public class SalesDemoGridService : ISalesDemoGridService
    {
        private readonly string? _connectionString;

        public SalesDemoGridService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(_connectionString))
                throw new ArgumentException("DefaultConnection string is not configured", nameof(configuration));
        }        public async Task<(IEnumerable<SalesDemoGrid> Data, int TotalRecords)> GetSalesDemoGridAsync(SalesDemoGridRequest request)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Clean up request data
                request.SearchText = request.SearchText == "string" ? null : request.SearchText;
                request.CustomerNames = request.CustomerNames?.Where(x => x != "string").ToArray();
                request.Statuses = request.Statuses?.Where(x => x != "string").ToArray();
                request.DemoApproaches = request.DemoApproaches?.Where(x => x != "string").ToArray();
                request.DemoOutcomes = request.DemoOutcomes?.Where(x => x != "string").ToArray();
                
                var parameters = new 
                {
                    p_request = JsonSerializer.Serialize(new
                    {
                        searchText = request.SearchText,
                        customerNames = request.CustomerNames ?? Array.Empty<string>(),
                        statuses = request.Statuses ?? Array.Empty<string>(),
                        demoApproaches = request.DemoApproaches ?? Array.Empty<string>(),
                        demoOutcomes = request.DemoOutcomes ?? Array.Empty<string>(),
                        pageNumber = request.PageNumber,
                        pageSize = request.PageSize,
                        orderBy = request.OrderBy?.ToLower() ?? "dateCreated",
                        orderDirection = request.OrderDirection?.ToUpper() ?? "DESC"
                    })
                };

                var result = await connection.QueryAsync<SalesDemoGrid>(
                    "SELECT * FROM fn_get_sales_demos_grid(@p_request::jsonb)",
                    parameters
                );

                var demos = result.ToList();
                var totalRecords = demos.FirstOrDefault()?.TotalRecords ?? 0;

                return (demos, totalRecords);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching demo grid data: {ex.Message}", ex);
            }
        }
    }
}
