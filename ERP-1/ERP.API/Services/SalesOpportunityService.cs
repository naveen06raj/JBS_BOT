using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using Dapper;
using ERP.API.Models;
using ERP.API.Models.DTOs;
using ERP.API.Helpers;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ERP.API.Services
{
    public class SalesOpportunityService : BaseDataService<SalesOpportunity>, ISalesOpportunityService
    {
        private readonly ILogger<SalesOpportunityService> _logger;

        public SalesOpportunityService(string connectionString, ILogger<SalesOpportunityService> logger)
            : base(connectionString, "sales_opportunities")
        {
            _logger = logger;
        }
        public async Task<IEnumerable<SalesOpportunity>> GetOpportunitiesAsync()
        {
            try
            {
                const string sql = @"
                    SELECT * FROM sales_opportunities 
                    WHERE isactive = true 
                    ORDER BY date_created DESC";
                using var connection = CreateConnection();                var opportunities = (await connection.QueryAsync<SalesOpportunity>(sql))?.ToList() ?? new List<SalesOpportunity>();
                _logger.LogInformation("Found {Count} opportunities", opportunities.Count);
                return opportunities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching opportunities: {Message}", ex.Message);
                throw;
            }
        }        public async Task<SalesOpportunity?> GetOpportunityByIdAsync(string opportunityId)
        {
            const string sql = @"
                SELECT * FROM sales_opportunities 
                WHERE opportunity_id = @OpportunityId AND isactive = true";

            try
            {
                using var connection = CreateConnection();                var opportunity = await connection.QuerySingleOrDefaultAsync<SalesOpportunity>(sql, new { OpportunityId = opportunityId });
                if (opportunity == null)
                {
                    _logger.LogInformation("Opportunity not found: {OpportunityId}", opportunityId);
                    return null;
                }
                
                _logger.LogInformation("Found opportunity: {OpportunityId}", opportunityId);
                return opportunity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving opportunity {OpportunityId}: {Message}", opportunityId, ex.Message);
                throw;
            }
        }        public async Task<int> CreateOpportunityAsync(SalesOpportunity opportunity)
        {
            using var connection = CreateConnection();
            try
            {
                opportunity.OpportunityId = await IdGenerator.GenerateOpportunityId(connection);
                if (string.IsNullOrEmpty(opportunity.OpportunityId))
                {
                    throw new InvalidOperationException("Failed to generate opportunity ID");
                }
                opportunity.IsActive = true;

                _logger.LogInformation("Generated opportunity ID: {OpportunityId}", opportunity.OpportunityId);

                const string sql = @"
                    INSERT INTO sales_opportunities (
                        status, expected_completion, opportunity_type, opportunity_for,
                        customer_id, customer_name, customer_type, opportunity_name,
                        opportunity_id, comments, isactive, lead_id, sales_representative_id,
                        contact_name, contact_mobile_no, user_created, date_created)
                    VALUES (
                        @Status, @ExpectedCompletion, @OpportunityType, @OpportunityFor,
                        @CustomerId, @CustomerName, @CustomerType, @OpportunityName,
                        @OpportunityId, @Comments, @IsActive, @LeadId, @SalesRepresentativeId,
                        @ContactName, @ContactMobileNo, 1, CURRENT_TIMESTAMP)
                    RETURNING id";

                var id = await connection.ExecuteScalarAsync<int>(sql, opportunity);
                _logger.LogInformation("Created opportunity {OpportunityId} with internal ID {Id}", opportunity.OpportunityId, id);
                return id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create opportunity: {Message}", ex.Message);
                throw;
            }
        }        public async Task<bool> UpdateOpportunityAsync(string opportunityId, SalesOpportunity opportunity)
        {
            if (opportunityId != opportunity.OpportunityId)
                throw new ArgumentException($"Path parameter opportunityId ({opportunityId}) does not match opportunity.OpportunityId ({opportunity.OpportunityId})", nameof(opportunityId));

            if (string.IsNullOrWhiteSpace(opportunity.OpportunityId) || !opportunity.OpportunityId.StartsWith("OPP"))
                throw new ArgumentException("Invalid opportunity Id format. Expected format: OPP followed by digits (e.g., OPP00001)", nameof(opportunity.OpportunityId));

            const string sql = @"
                UPDATE sales_opportunities SET
                    status = @Status,
                    expected_completion = @ExpectedCompletion,
                    opportunity_type = @OpportunityType,
                    opportunity_for = @OpportunityFor,
                    customer_id = @CustomerId,
                    customer_name = @CustomerName,
                    customer_type = @CustomerType,
                    opportunity_name = @OpportunityName,
                    comments = @Comments,
                    lead_id = @LeadId,
                    sales_representative_id = @SalesRepresentativeId,
                    contact_name = @ContactName,
                    contact_mobile_no = @ContactMobileNo,
                    user_updated = 1,
                    date_updated = CURRENT_TIMESTAMP
                WHERE opportunity_id = @OpportunityId AND isactive = true";

            using var connection = CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, opportunity);
            return rowsAffected > 0;
        }        public async Task<bool> DeleteOpportunityAsync(string opportunityId)
        {
            try
            {
                const string sql = @"
                    UPDATE sales_opportunities SET 
                        isactive = false,
                        user_updated = 1,
                        date_updated = CURRENT_TIMESTAMP
                    WHERE opportunity_id = @OpportunityId AND isactive = true";

                using var connection = CreateConnection();
                var rowsAffected = await connection.ExecuteAsync(sql, new { OpportunityId = opportunityId });

                _logger.LogInformation(rowsAffected > 0 
                    ? "Deleted opportunity: {OpportunityId}"
                    : "No opportunity found to delete: {OpportunityId}", 
                    opportunityId);

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting opportunity {OpportunityId}: {Message}", opportunityId, ex.Message);
                throw;
            }
        }        public async Task<IEnumerable<SalesOpportunityDto>> GetOpportunitiesByLeadIdAsync(int leadId)
        {
            try
            {
                const string sql = "SELECT * FROM fn_getopportunitiesbyleadid(@LeadId)";
                using var connection = CreateConnection();
                var opportunities = await connection.QueryAsync<SalesOpportunityDto>(sql, new { LeadId = leadId });
                _logger.LogInformation("Found {Count} opportunities for lead {LeadId}", opportunities?.Count() ?? 0, leadId);
                return opportunities ?? Enumerable.Empty<SalesOpportunityDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting opportunities for lead {LeadId}: {Message}", leadId, ex.Message);
                throw;
            }
        }
        public async Task<(IEnumerable<SalesOpportunityGridResult> Results, int TotalRecords)> GetOpportunitiesGridAsync(
            string? searchText = null,
            string[]? customerNames = null,
            string[]? territories = null,
            string[]? statuses = null,
            string[]? stages = null,
            string[]? opportunityTypes = null,
            int pageNumber = 1,
            int pageSize = 10,
            string? orderBy = "date_created",
            string? orderDirection = "DESC")
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("p_search_text", searchText);
                parameters.Add("p_customer_names", customerNames?.Length > 0 ? customerNames : null, DbType.Object);
                parameters.Add("p_territories", territories?.Length > 0 ? territories : null, DbType.Object);
                parameters.Add("p_statuses", statuses?.Length > 0 ? statuses : null, DbType.Object);
                parameters.Add("p_stages", stages?.Length > 0 ? stages : null, DbType.Object);
                parameters.Add("p_opportunity_types", opportunityTypes?.Length > 0 ? opportunityTypes : null, DbType.Object);
                parameters.Add("p_page_number", pageNumber);
                parameters.Add("p_page_size", pageSize);
                parameters.Add("p_order_by", orderBy);
                parameters.Add("p_order_direction", orderDirection); using var connection = CreateConnection();
                var results = await connection.QueryAsync<SalesOpportunityGridResult>(
                    "SELECT * FROM fn_get_opportunities_with_pagination(" +
                    "p_search_text => @p_search_text, " +
                    "p_customer_names => @p_customer_names, " +
                    "p_territories => @p_territories, " +
                    "p_statuses => @p_statuses, " +
                    "p_stages => @p_stages, " +
                    "p_opportunity_types => @p_opportunity_types, " +
                    "p_page_number => @p_page_number, " +
                    "p_page_size => @p_page_size, " +
                    "p_order_by => @p_order_by, " +
                    "p_order_direction => @p_order_direction)",
                    parameters
                );

                var resultsList = results.ToList();
                var totalRecords = resultsList.FirstOrDefault()?.TotalRecords ?? 0;

                _logger.LogInformation("Retrieved {Count} opportunities out of {Total} total records", resultsList.Count, totalRecords);                return (resultsList, totalRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOpportunitiesGridAsync: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<IEnumerable<OpportunityCardDto>> GetOpportunityCardsAsync()
        {
            try
            {
                const string sql = "SELECT * FROM sp_get_opportunity_cards_count()";
                using var connection = CreateConnection();
                var cards = await connection.QueryAsync<OpportunityCardDto>(sql);
                
                _logger.LogInformation("Retrieved opportunity cards data with {Count} statuses", cards?.Count() ?? 0);
                return cards ?? Enumerable.Empty<OpportunityCardDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting opportunity cards data: {Message}", ex.Message);
                throw;
            }
        }

        protected override string GenerateInsertQuery()
        {
            return @"
                INSERT INTO sales_opportunities (
                    status, expected_completion, opportunity_type, opportunity_for,
                    customer_id, customer_name, customer_type, opportunity_name,
                    opportunity_id, comments, isactive, lead_id, sales_representative_id,
                    contact_name, contact_mobile_no, user_created, date_created)
                VALUES (
                    @Status, @ExpectedCompletion, @OpportunityType, @OpportunityFor,
                    @CustomerId, @CustomerName, @CustomerType, @OpportunityName,
                    @OpportunityId, @Comments, @IsActive, @LeadId, @SalesRepresentativeId,
                    @ContactName, @ContactMobileNo, 1, CURRENT_TIMESTAMP)
                RETURNING id";
        }

        protected override string GenerateUpdateQuery()
        {
            return @"
                UPDATE sales_opportunities SET
                    status = @Status,
                    expected_completion = @ExpectedCompletion,
                    opportunity_type = @OpportunityType,
                    opportunity_for = @OpportunityFor,
                    customer_id = @CustomerId,
                    customer_name = @CustomerName,
                    customer_type = @CustomerType,
                    opportunity_name = @OpportunityName,
                    opportunity_id = @OpportunityId,
                    comments = @Comments,
                    lead_id = @LeadId,
                    sales_representative_id = @SalesRepresentativeId,
                    contact_name = @ContactName,
                    contact_mobile_no = @ContactMobileNo,
                    user_updated = 1,
                    date_updated = CURRENT_TIMESTAMP
                WHERE id = @Id AND isactive = true";
        }

        public async Task<SalesOpportunity?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT * FROM sales_opportunities 
                WHERE id = @Id AND isactive = true";

            try
            {
                using var connection = CreateConnection();
                var opportunity = await connection.QuerySingleOrDefaultAsync<SalesOpportunity>(sql, new { Id = id });
                
                if (opportunity == null)
                {
                    _logger.LogWarning("Opportunity with ID {Id} not found", id);
                    return null;
                }

                _logger.LogInformation("Retrieved opportunity {Id}", id);
                return opportunity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving opportunity {Id}: {Message}", id, ex.Message);
                throw;
            }
        }
    }
}