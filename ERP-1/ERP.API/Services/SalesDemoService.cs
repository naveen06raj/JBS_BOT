using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using ERP.API.Models;
using ERP.API.Models.DTOs;
using ERP.API.Helpers;
using Microsoft.Extensions.Logging;

namespace ERP.API.Services
{
    public class SalesDemoService : ISalesDemoService
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<SalesDemoService> _logger;

        public SalesDemoService(IDbConnection connection, ILogger<SalesDemoService> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<IEnumerable<SalesDemo>> GetDemosAsync()
        {
            const string sql = @"                SELECT d.*, u.username as PresenterName 
                FROM sales_demos d
                LEFT JOIN users u ON d.presenter_id = u.user_id 
                ORDER BY d.date_created DESC";

            return await _connection.QueryAsync<SalesDemo>(sql);
        }

        public async Task<SalesDemo?> GetDemoByIdAsync(int id)
        {
            try
            {
                const string sql = @"                    SELECT d.*, u.username as PresenterName
                    FROM sales_demos d
                    LEFT JOIN users u ON d.presenter_id = u.user_id
                    WHERE d.id = @Id";

                return await _connection.QueryFirstOrDefaultAsync<SalesDemo>(sql, new { Id = id });
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error getting demo with ID {Id}: {Message}", id, ex.Message);
                throw;
            }
        }
        private static string GetSortColumn(string? requestedColumn)
        {
            return requestedColumn?.ToLower() switch
            {
                "id" => "id",
                "customername" => "customer_name",
                "demoname" => "demo_name",
                "demotype" => "demo_type",
                "status" => "status",
                "demodatetime" => "demo_date",
                "democontact" => "demo_contact",
                "demoapproach" => "demo_approach",
                "demooutcome" => "demo_outcome",
                "demofeedback" => "demo_feedback",
                "comments" => "comments",
                "opportunityid" => "opportunity_id",
                "presenterid" => "presenter_id",
                "presentername" => "u.username",
                "datecreated" => "date_created",
                "dateupdated" => "date_updated",
                "addressid" => "address_id",
                "customerid" => "customer_id",
                "userid" => "user_id",
                _ => "date_created"
            };
        }

        public async Task<int> CreateDemoAsync(SalesDemo demo)
        {
            if (demo == null)
            {
                throw new ArgumentNullException(nameof(demo));
            }

            if (string.IsNullOrEmpty(demo.CustomerName))
            {
                throw new ArgumentException("Customer name is required");
            }

            if (string.IsNullOrEmpty(demo.DemoName))
            {
                throw new ArgumentException("Demo name is required");
            }

            demo.DateCreated = DateTime.UtcNow;
            demo.UserCreated = 1; // Set to appropriate user ID

            const string sql = @"
                INSERT INTO sales_demos (
                    user_created, date_created, demo_contact, status, 
                    customer_id, customer_name, demo_name, demo_approach,
                    demo_outcome, demo_feedback, comments, opportunity_id, 
                    presenter_id
                )
                VALUES (
                    @UserCreated, @DateCreated, @DemoContact, @Status,
                    @CustomerId, @CustomerName, @DemoName, @DemoApproach,
                    @DemoOutcome, @DemoFeedback, @Comments, @OpportunityId,
                    @PresenterId
                )
                RETURNING id";

            return await _connection.ExecuteScalarAsync<int>(sql, demo);
        }

        public async Task<bool> UpdateDemoAsync(int id, SalesDemo demo)
        {
            if (demo == null)
            {
                throw new ArgumentNullException(nameof(demo));
            }

            if (id != demo.Id)
            {
                throw new ArgumentException("ID mismatch");
            }

            if (string.IsNullOrEmpty(demo.CustomerName))
            {
                throw new ArgumentException("Customer name is required");
            }

            if (string.IsNullOrEmpty(demo.DemoName))
            {
                throw new ArgumentException("Demo name is required");
            }

            demo.DateUpdated = DateTime.UtcNow;
            demo.UserUpdated = 1; // Set to appropriate user ID

            const string sql = @"
                UPDATE sales_demos SET
                    user_updated = @UserUpdated,
                    date_updated = @DateUpdated,
                    demo_contact = @DemoContact,
                    status = @Status,
                    customer_id = @CustomerId,
                    customer_name = @CustomerName,
                    demo_name = @DemoName,
                    demo_approach = @DemoApproach,
                    demo_outcome = @DemoOutcome,
                    demo_feedback = @DemoFeedback,
                    comments = @Comments,
                    opportunity_id = @OpportunityId,
                    presenter_id = @PresenterId
                WHERE id = @Id";

            var rowsAffected = await _connection.ExecuteAsync(sql, demo);
            return rowsAffected > 0;
        }
        public async Task<bool> DeleteDemoAsync(int id)
        {
            const string sql = @"
                DELETE FROM sales_demos 
                WHERE id = @Id";

            var rowsAffected = await _connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<IEnumerable<SalesDemo>> GetDemosByOpportunityIdAsync(int opportunityId)
        {
            const string sql = @"                SELECT d.*, u.username as PresenterName
                FROM sales_demos d
                LEFT JOIN users u ON d.presenter_id = u.user_id
                WHERE d.opportunity_id = @OpportunityId 
                ORDER BY d.date_created DESC";

            return await _connection.QueryAsync<SalesDemo>(sql, new { OpportunityId = opportunityId });
        }
public async Task<DemoCardsDto> GetDemoCardsAsync()
{
    try
    {
        const string sql = "SELECT * FROM sp_get_demo_cards_count()";
        var cards = await _connection.QueryFirstOrDefaultAsync<DemoCardsDto>(sql);
        return cards ?? new DemoCardsDto();
    }
    catch (Exception ex)
    {
        throw new Exception($"Error getting Demo cards data: {ex.Message}", ex);
    }
}

        private IDbConnection CreateConnection()
        {
            throw new NotImplementedException();
        }
    }
}
