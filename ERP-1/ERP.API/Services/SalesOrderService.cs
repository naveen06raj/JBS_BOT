using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using ERP.API.Models;
using ERP.API.Models.DTOs;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ERP.API.Services
{
    public class SalesOrderService : ISalesOrderService
    {        private readonly string? _connectionString;

        public SalesOrderService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(_connectionString))
                throw new ArgumentException("DefaultConnection string is not configured", nameof(configuration));
        }

        public async Task<IEnumerable<SalesOrderGrid>> GetAllSalesOrdersAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<SalesOrderGrid>(
                "SELECT so.id, so.order_id as OrderId, c.name as CustomerName, " +
                "so.order_date as OrderDate, so.expected_delivery_date as ExpectedDeliveryDate, " +
                "so.status, so.po_id as PoId, so.grand_total as GrandTotal " +
                "FROM sales_orders so " +
                "LEFT JOIN sales_customers c ON so.customer_id = c.id " +
                "ORDER BY so.order_date DESC");
        }

        public async Task<SalesOrder> GetSalesOrderByIdAsync(int id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<SalesOrder>(
                "SELECT * FROM sales_orders WHERE id = @Id",
                new { Id = id });
        }        public async Task<int> CreateSalesOrderAsync(SalesOrder salesOrder)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT id FROM fn_create_sales_order(
                    @CustomerId, @OrderDate, @ExpectedDeliveryDate,
                    @Status, @QuotationId, @PoId, @AcceptanceDate,
                    @TotalAmount, @TaxAmount, @GrandTotal, @Notes,
                    @UserCreated
                )";

            return await connection.QueryFirstOrDefaultAsync<int>(sql, salesOrder);
        }        public async Task<SalesOrder> UpdateSalesOrderAsync(SalesOrder salesOrder)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                UPDATE sales_orders
                SET order_id = @OrderId,
                    customer_id = @CustomerId,
                    order_date = @OrderDate,
                    expected_delivery_date = @ExpectedDeliveryDate,
                    status = @Status,
                    quotation_id = @QuotationId,
                    po_id = @PoId,
                    acceptance_date = @AcceptanceDate,
                    total_amount = @TotalAmount,
                    tax_amount = @TaxAmount,
                    grand_total = @GrandTotal,
                    notes = @Notes,
                    user_updated = @UserUpdated,
                    date_updated = CURRENT_TIMESTAMP
                WHERE id = @Id
                RETURNING *";

            return await connection.QueryFirstOrDefaultAsync<SalesOrder>(sql, salesOrder);
        }

        public async Task<bool> DeleteSalesOrderAsync(int id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var rowsAffected = await connection.ExecuteAsync(
                "DELETE FROM sales_orders WHERE id = @Id",
                new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<QuotationWithOrderResponse> GetQuotationByIdAsync(int id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                WITH quotation_data AS (
                    SELECT 
                        q.id,
                        q.user_created,
                        q.date_created,
                        q.user_updated,
                        q.date_updated,
                        q.quotation_type,
                        q.order_type,
                        q.quotation_date,
                        q.status as quotation_status,
                        q.version,
                        q.terms,
                        q.valid_till,
                        q.quotation_for,
                        q.lost_reason,
                        q.customer_id,
                        q.comments as quotation_comments,
                        q.delivery_within,
                        q.delivery_after,
                        q.is_active,
                        q.quotation_id,
                        q.opportunity_id,
                        q.lead_id,
                        q.taxes,
                        q.delivery,
                        q.payment,
                        q.warranty,
                        q.freight_charge,
                        q.is_current,
                        q.parent_sales_quotations_id,
                        c.name as customer_name,
                        CONCAT_WS(', ', 
                            NULLIF(sl.door_no, ''),
                            NULLIF(sl.street, ''),
                            NULLIF(sl.landmark, ''),
                            NULLIF(sl.city, ''),
                            NULLIF(sl.district, ''),
                            NULLIF(sl.state, ''),
                            NULLIF(sl.pincode, '')
                        ) as customer_address,
                        COALESCE(sc.mobile_no, sl.contact_mobile_no) as customer_mobile,
                        COALESCE(sc.email, sl.email) as customer_email
                    FROM sales_quotations q
                    LEFT JOIN sales_customers c ON q.customer_id = c.id
                    LEFT JOIN sales_lead sl ON q.lead_id = sl.id
                    LEFT JOIN sales_contacts sc ON sc.sales_lead_id = q.lead_id
                    WHERE q.id = @Id
                )
                SELECT 
                    qd.*,
                    so.id as sales_order_id,
                    so.order_id,
                    so.order_date,
                    so.expected_delivery_date,
                    so.status as order_status,
                    so.po_id,
                    so.acceptance_date,
                    so.total_amount,
                    so.tax_amount,
                    so.grand_total,
                    so.notes as order_notes
                FROM quotation_data qd
                LEFT JOIN sales_orders so ON so.quotation_id = qd.id";

            var result = await connection.QueryAsync<QuotationDetails, SalesOrderDetails, QuotationWithOrderResponse>(
                sql,
                (quotation, salesOrder) => new QuotationWithOrderResponse 
                { 
                    Quotation = quotation, 
                    SalesOrder = salesOrder 
                },
                new { Id = id },
                splitOn: "sales_order_id"
            );

            return result.FirstOrDefault() ?? new QuotationWithOrderResponse 
            { 
                Quotation = new QuotationDetails(),
                SalesOrder = null 
            };
        }
    }
}
