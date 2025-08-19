using ERP.API.Models.DealGrid;
using System.Threading.Tasks;
using Npgsql;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Configuration;

namespace ERP.API.Services
{
    public interface ISalesDealGridService
    {
        Task<DealGridPaginatedResponse> GetDealsGridAsync(DealGridRequest request);
    }    public class SalesDealGridService : ISalesDealGridService
    {
        private readonly string _connectionString;

        public SalesDealGridService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException(nameof(configuration), "DefaultConnection string is not configured");
        }

        public async Task<DealGridPaginatedResponse> GetDealsGridAsync(DealGridRequest request)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var parameters = new
                {
                    p_request = JsonSerializer.Serialize(new
                    {
                        searchText = request.SearchText ?? string.Empty,
                        customerNames = request.CustomerNames ?? new List<string>(),
                        statuses = request.Statuses ?? new List<string>(),
                        dealIds = request.DealIds ?? new List<string>(),
                        pageNumber = request.PageNumber,
                        pageSize = request.PageSize,
                        orderBy = request.OrderBy ?? "date_created",
                        orderDirection = request.OrderDirection ?? "DESC"
                    })
                };

                var result = await connection.QueryFirstAsync<DealGridPaginatedResponse>(
                    "SELECT * FROM fn_get_sales_deals_grid(@p_request::json) AS (deals json, total_records integer)",
                    parameters
                );

                return result ?? new DealGridPaginatedResponse { Deals = new List<DealGridResponse>(), TotalRecords = 0 };
            }
        }
    }
}
