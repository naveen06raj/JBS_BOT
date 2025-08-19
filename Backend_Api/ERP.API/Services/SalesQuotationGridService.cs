using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ERP.API.Models;

namespace ERP.API.Services
{
    public interface ISalesQuotationGridService
    {
        Task<(IEnumerable<SalesQuotationGrid> Data, int TotalRecords)> GetSalesQuotationGridAsync(SalesQuotationGridRequest request);
    }

    public class SalesQuotationGridService : ISalesQuotationGridService
    {
        private readonly string? _connectionString;

        public SalesQuotationGridService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(_connectionString))
                throw new ArgumentException("DefaultConnection string is not configured", nameof(configuration));
        }

        public async Task<(IEnumerable<SalesQuotationGrid> Data, int TotalRecords)> GetSalesQuotationGridAsync(SalesQuotationGridRequest request)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var parameters = new
            {
                p_request = System.Text.Json.JsonSerializer.Serialize(new
                {
                    searchText = request.SearchText,
                    customerNames = request.CustomerNames,
                    statuses = request.Statuses,
                    quotationIds = request.QuotationIds,
                    pageNumber = request.PageNumber,
                    pageSize = request.PageSize,
                    orderBy = request.OrderBy.ToLower(),
                    orderDirection = request.OrderDirection.ToUpper()
                })
            }; var result = await connection.QueryAsync<dynamic>(
                @"SELECT * FROM fn_get_sales_quotations_grid(@p_request::json) 
                  AS (total_records INTEGER,
                      id INTEGER,
                      user_created INTEGER,
                      date_created TIMESTAMP,
                      user_updated INTEGER,
                      date_updated TIMESTAMP,
                      version VARCHAR,
                      terms VARCHAR,
                      valid_till TIMESTAMP,
                      quotation_for VARCHAR,
                      status VARCHAR,
                      lost_reason VARCHAR,
                      customer_id INTEGER,
                      quotation_type VARCHAR,
                      quotation_date TIMESTAMP,
                      order_type VARCHAR,
                      comments VARCHAR,
                      delivery_within VARCHAR,
                      delivery_after VARCHAR,
                      is_active BOOLEAN,
                      quotation_id VARCHAR,
                      opportunity_id INTEGER,
                      lead_id INTEGER,
                      customer_name VARCHAR,
                      taxes VARCHAR,
                      delivery VARCHAR,
                      payment VARCHAR,
                      warranty VARCHAR,
                      freight_charge VARCHAR,
                      is_current BOOLEAN,
                      parent_sales_quotations_id INTEGER,
                      products json)",
                parameters);

            if (result == null || !result.Any())
                return (new List<SalesQuotationGrid>(), 0);

            var firstRow = result.First();
            var totalRecords = (int)firstRow.total_records;

            var gridData = result.Select(row => new SalesQuotationGrid
            {
                Id = row.id,
                UserCreated = row.user_created,
                DateCreated = row.date_created,
                UserUpdated = row.user_updated,
                DateUpdated = row.date_updated,
                Version = row.version,
                Terms = row.terms,
                ValidTill = row.valid_till,
                QuotationFor = row.quotation_for,
                Status = row.status,
                LostReason = row.lost_reason,
                CustomerId = row.customer_id,
                QuotationType = row.quotation_type,
                QuotationDate = row.quotation_date,
                OrderType = row.order_type,
                Comments = row.comments,
                DeliveryWithin = row.delivery_within,
                DeliveryAfter = row.delivery_after,
                IsActive = row.is_active,
                QuotationId = row.quotation_id,
                OpportunityId = row.opportunity_id,
                LeadId = row.lead_id,
                CustomerName = row.customer_name,
                Taxes = row.taxes,
                Delivery = row.delivery,
                Payment = row.payment,
                Warranty = row.warranty,
                FreightCharge = row.freight_charge,
                IsCurrent = row.is_current,
                ParentSalesQuotationsId = row.parent_sales_quotations_id,
                Products = row.products
            });

            return (gridData, totalRecords);
        }
    }
}
