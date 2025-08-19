using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using ERP.API.Models;
using System.Linq;

namespace ERP.API.Services
{
    public class SalesQuotationService : BaseDataService<SalesQuotation>
    {
        public SalesQuotationService(string connectionString) 
            : base(connectionString, "sales_quotations")
        {
            ValidateDatabaseObjects().Wait();
            EnsureStoredProcedures().Wait();
        }

        private async Task ValidateDatabaseObjects()
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                // Check if table exists
                var tableExists = await connection.QuerySingleOrDefaultAsync<bool>(
                    @"SELECT EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_schema = 'public' 
                        AND table_name = 'sales_quotations'
                    );");

                if (!tableExists)
                {
                    throw new Exception("Table sales_quotations does not exist");
                }

                // Check if stored procedures exist
                var procedureNames = new[] { 
                    "get_all_quotations", 
                    "get_quotation_by_id", 
                    "create_quotation", 
                    "update_quotation", 
                    "delete_quotation",
                    "get_quotations_by_opportunity",
                    "get_quotations_by_customer"
                };

                var existingProcedures = await connection.QueryAsync<string>(
                    @"SELECT proname 
                    FROM pg_proc 
                    WHERE proname = ANY(@names);",
                    new { names = procedureNames });

                var missingProcedures = procedureNames.Except(existingProcedures);
                if (missingProcedures.Any())
                {
                    await EnsureStoredProcedures();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error validating database objects: {ex.Message}", ex);
            }
        }

        private async Task EnsureStoredProcedures()
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                // Drop existing stored procedures first to ensure clean creation
                var dropSql = @"
                    DROP FUNCTION IF EXISTS get_all_quotations();
                    DROP FUNCTION IF EXISTS get_quotation_by_id(INT);
                    DROP FUNCTION IF EXISTS get_quotations_by_opportunity(INT);
                    DROP FUNCTION IF EXISTS get_quotations_by_customer(INT);
                    DROP FUNCTION IF EXISTS create_quotation(
                        INT, VARCHAR, VARCHAR, TIMESTAMP WITH TIME ZONE, VARCHAR,
                        VARCHAR, VARCHAR, INT, VARCHAR, TIMESTAMP WITH TIME ZONE,
                        VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, INT, INT,
                        VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, INT);
                    DROP FUNCTION IF EXISTS update_quotation(
                        INT, INT, VARCHAR, VARCHAR, TIMESTAMP WITH TIME ZONE, VARCHAR,
                        VARCHAR, VARCHAR, INT, VARCHAR, TIMESTAMP WITH TIME ZONE,
                        VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, INT,
                        VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, INT);
                ";
                await connection.ExecuteAsync(dropSql);

                // Create get_all_quotations
                var getAllSql = @"
                    CREATE OR REPLACE FUNCTION get_all_quotations()
                    RETURNS TABLE (
                        id INT,
                        user_created INT,
                        date_created TIMESTAMP,
                        user_updated INT,
                        date_updated TIMESTAMP,
                        version VARCHAR(255),
                        terms VARCHAR(255),
                        valid_till TIMESTAMP,
                        quotation_for VARCHAR(255),
                        status VARCHAR(255),
                        lost_reason VARCHAR(255),
                        customer_id INT,
                        quotation_type VARCHAR(255),
                        quotation_date TIMESTAMP,
                        order_type VARCHAR(255),
                        comments VARCHAR(255),
                        delivery_within VARCHAR(255),
                        delivery_after VARCHAR(255),
                        is_active BOOLEAN,
                        quotation_id VARCHAR(255),
                        opportunity_id INT,
                        customer_name VARCHAR(255),
                        taxes VARCHAR(255),
                        delivery VARCHAR(255),
                        payment VARCHAR(255),
                        warranty VARCHAR(255),
                        freight_charge VARCHAR(255),
                        is_current BOOLEAN,
                        parent_sales_quotations_id INT
                    ) AS $$
                    BEGIN    
                        RETURN QUERY 
                        SELECT 
                            sq.id,
                            sq.user_created,
                            sq.date_created,
                            sq.user_updated,
                            sq.date_updated,
                            sq.version,
                            sq.terms,
                            sq.valid_till,
                            sq.quotation_for,
                            sq.status,
                            sq.lost_reason,
                            sq.customer_id,
                            sq.quotation_type,
                            sq.quotation_date,
                            sq.order_type,
                            sq.comments,
                            sq.delivery_within,
                            sq.delivery_after,
                            sq.is_active,
                            sq.quotation_id,
                            sq.opportunity_id,
                            sq.customer_name,
                            sq.taxes,
                            sq.delivery,
                            sq.payment,
                            sq.warranty,
                            sq.freight_charge,
                            sq.is_current,
                            sq.parent_sales_quotations_id
                        FROM public.sales_quotations sq 
                        WHERE sq.is_active = true
                        ORDER BY sq.date_created DESC;
                    END;
                    $$ LANGUAGE plpgsql;
                ";
                await connection.ExecuteAsync(getAllSql);

                // Create get_quotation_by_id
                var getByIdSql = @"
                    CREATE OR REPLACE FUNCTION get_quotation_by_id(p_id INT)
                    RETURNS TABLE (
                        id INT,
                        user_created INT,
                        date_created TIMESTAMP,
                        user_updated INT,
                        date_updated TIMESTAMP,
                        version VARCHAR(255),
                        terms VARCHAR(255),
                        valid_till TIMESTAMP,
                        quotation_for VARCHAR(255),
                        status VARCHAR(255),
                        lost_reason VARCHAR(255),
                        customer_id INT,
                        quotation_type VARCHAR(255),
                        quotation_date TIMESTAMP,
                        order_type VARCHAR(255),
                        comments VARCHAR(255),
                        delivery_within VARCHAR(255),
                        delivery_after VARCHAR(255),
                        is_active BOOLEAN,
                        quotation_id VARCHAR(255),
                        opportunity_id INT,
                        customer_name VARCHAR(255),
                        taxes VARCHAR(255),
                        delivery VARCHAR(255),
                        payment VARCHAR(255),
                        warranty VARCHAR(255),
                        freight_charge VARCHAR(255),
                        is_current BOOLEAN,
                        parent_sales_quotations_id INT
                    ) AS $$
                    BEGIN    
                        RETURN QUERY 
                        SELECT 
                            sq.id,
                            sq.user_created,
                            sq.date_created,
                            sq.user_updated,
                            sq.date_updated,
                            sq.version,
                            sq.terms,
                            sq.valid_till,
                            sq.quotation_for,
                            sq.status,
                            sq.lost_reason,
                            sq.customer_id,
                            sq.quotation_type,
                            sq.quotation_date,
                            sq.order_type,
                            sq.comments,
                            sq.delivery_within,
                            sq.delivery_after,
                            sq.is_active,
                            sq.quotation_id,
                            sq.opportunity_id,
                            sq.customer_name,
                            sq.taxes,
                            sq.delivery,
                            sq.payment,
                            sq.warranty,
                            sq.freight_charge,
                            sq.is_current,
                            sq.parent_sales_quotations_id
                        FROM public.sales_quotations sq 
                        WHERE sq.id = p_id AND sq.is_active = true;
                    END;
                    $$ LANGUAGE plpgsql;
                ";
                await connection.ExecuteAsync(getByIdSql);

                // Create create_quotation stored procedure
                var createQuotationSql = @"
                    CREATE OR REPLACE FUNCTION create_quotation(
                        p_user_created INT,
                        p_version VARCHAR,
                        p_terms VARCHAR,
                        p_valid_till TIMESTAMP,
                        p_quotation_for VARCHAR,
                        p_status VARCHAR,
                        p_lost_reason VARCHAR,
                        p_customer_id INT,
                        p_quotation_type VARCHAR,
                        p_quotation_date TIMESTAMP,
                        p_order_type VARCHAR,
                        p_comments VARCHAR,
                        p_delivery_within VARCHAR,
                        p_delivery_after VARCHAR,
                        p_quotation_id VARCHAR,
                        p_opportunity_id INT,
                        p_customer_name VARCHAR,
                        p_taxes VARCHAR,
                        p_delivery VARCHAR,
                        p_payment VARCHAR,
                        p_warranty VARCHAR,
                        p_freight_charge VARCHAR,
                        p_parent_sales_quotations_id INT
                    )
                    RETURNS INT AS $$
                    DECLARE
                        new_id INT;
                    BEGIN
                        INSERT INTO sales_quotations (
                            user_created,
                            date_created,
                            version,
                            terms,
                            valid_till,
                            quotation_for,
                            status,
                            lost_reason,
                            customer_id,
                            quotation_type,
                            quotation_date,
                            order_type,
                            comments,
                            delivery_within,
                            delivery_after,
                            is_active,
                            quotation_id,
                            opportunity_id,
                            customer_name,
                            taxes,
                            delivery,
                            payment,
                            warranty,
                            freight_charge,
                            is_current,
                            parent_sales_quotations_id
                        )
                        VALUES (
                            p_user_created,
                            CURRENT_TIMESTAMP,
                            p_version,
                            p_terms,
                            p_valid_till,
                            p_quotation_for,
                            p_status,
                            p_lost_reason,
                            p_customer_id,
                            p_quotation_type,
                            p_quotation_date,
                            p_order_type,
                            p_comments,
                            p_delivery_within,
                            p_delivery_after,
                            true,
                            p_quotation_id,
                            p_opportunity_id,
                            p_customer_name,
                            p_taxes,
                            p_delivery,
                            p_payment,
                            p_warranty,
                            p_freight_charge,
                            true,
                            p_parent_sales_quotations_id
                        )
                        RETURNING id INTO new_id;

                        RETURN new_id;
                    END;
                    $$ LANGUAGE plpgsql;
                ";
                await connection.ExecuteAsync(createQuotationSql);

                // Create get_quotations_by_opportunity
                var getByOpportunitySql = @"
                    CREATE OR REPLACE FUNCTION get_quotations_by_opportunity(p_opportunity_id INT)
                    RETURNS TABLE (LIKE sales_quotations) AS $$
                    BEGIN    
                        RETURN QUERY 
                        SELECT *
                        FROM sales_quotations sq 
                        WHERE sq.opportunity_id = p_opportunity_id 
                        AND sq.is_active = true
                        ORDER BY sq.date_created DESC;
                    END;
                    $$ LANGUAGE plpgsql;
                ";
                await connection.ExecuteAsync(getByOpportunitySql);

                // Create get_quotations_by_customer
                var getByCustomerSql = @"
                    CREATE OR REPLACE FUNCTION get_quotations_by_customer(p_customer_id INT)
                    RETURNS TABLE (LIKE sales_quotations) AS $$
                    BEGIN    
                        RETURN QUERY 
                        SELECT *
                        FROM sales_quotations sq 
                        WHERE sq.customer_id = p_customer_id 
                        AND sq.is_active = true
                        ORDER BY sq.date_created DESC;
                    END;
                    $$ LANGUAGE plpgsql;
                ";
                await connection.ExecuteAsync(getByCustomerSql);

                // Create update_quotation stored procedure
                var updateQuotationSql = @"
                    CREATE OR REPLACE FUNCTION update_quotation(
                        p_id INT,
                        p_user_updated INT,
                        p_version VARCHAR(255),
                        p_terms VARCHAR(255),
                        p_valid_till TIMESTAMP WITH TIME ZONE,
                        p_quotation_for VARCHAR(255),
                        p_status VARCHAR(255),
                        p_lost_reason VARCHAR(255),
                        p_customer_id INT,
                        p_quotation_type VARCHAR(255),
                        p_quotation_date TIMESTAMP WITH TIME ZONE,
                        p_order_type VARCHAR(255),
                        p_comments VARCHAR(255),
                        p_delivery_within VARCHAR(255),
                        p_delivery_after VARCHAR(255),
                        p_quotation_id VARCHAR(255),
                        p_opportunity_id INT,
                        p_customer_name VARCHAR(255),
                        p_taxes VARCHAR(255),
                        p_delivery VARCHAR(255),
                        p_payment VARCHAR(255),
                        p_warranty VARCHAR(255),
                        p_freight_charge VARCHAR(255),
                        p_parent_sales_quotations_id INT
                    )
                    RETURNS BOOLEAN AS $$
                    DECLARE
                        updated_rows INT;
                        exists_but_inactive BOOLEAN;
                        exists_at_all BOOLEAN;
                    BEGIN
                        -- Check if the record exists at all
                        SELECT EXISTS(SELECT 1 FROM sales_quotations WHERE id = p_id)
                        INTO exists_at_all;

                        -- Check if record exists but is inactive
                        SELECT EXISTS(SELECT 1 FROM sales_quotations WHERE id = p_id AND is_active = false)
                        INTO exists_but_inactive;

                        -- Log the state
                        RAISE NOTICE 'Updating quotation ID: %, Exists: %, Is Inactive: %', 
                            p_id, exists_at_all, exists_but_inactive;

                        IF NOT exists_at_all THEN
                            RAISE EXCEPTION 'Quotation with ID % does not exist', p_id;
                        END IF;

                        IF exists_but_inactive THEN
                            RAISE EXCEPTION 'Quotation with ID % exists but is inactive', p_id;
                        END IF;

                        UPDATE sales_quotations 
                        SET 
                            user_updated = p_user_updated,
                            date_updated = CURRENT_TIMESTAMP,
                            version = COALESCE(NULLIF(p_version, ''), version),
                            terms = COALESCE(NULLIF(p_terms, ''), terms),
                            valid_till = COALESCE(p_valid_till, valid_till),
                            quotation_for = COALESCE(NULLIF(p_quotation_for, ''), quotation_for),
                            status = COALESCE(NULLIF(p_status, ''), status),
                            lost_reason = COALESCE(NULLIF(p_lost_reason, ''), lost_reason),
                            customer_id = COALESCE(p_customer_id, customer_id),
                            quotation_type = COALESCE(NULLIF(p_quotation_type, ''), quotation_type),
                            quotation_date = COALESCE(p_quotation_date, quotation_date),
                            order_type = COALESCE(NULLIF(p_order_type, ''), order_type),
                            comments = COALESCE(NULLIF(p_comments, ''), comments),
                            delivery_within = COALESCE(NULLIF(p_delivery_within, ''), delivery_within),
                            delivery_after = COALESCE(NULLIF(p_delivery_after, ''), delivery_after),
                            quotation_id = COALESCE(NULLIF(p_quotation_id, ''), quotation_id),
                            opportunity_id = COALESCE(p_opportunity_id, opportunity_id),
                            customer_name = COALESCE(NULLIF(p_customer_name, ''), customer_name),
                            taxes = COALESCE(NULLIF(p_taxes, ''), taxes),
                            delivery = COALESCE(NULLIF(p_delivery, ''), delivery),
                            payment = COALESCE(NULLIF(p_payment, ''), payment),
                            warranty = COALESCE(NULLIF(p_warranty, ''), warranty),
                            freight_charge = COALESCE(NULLIF(p_freight_charge, ''), freight_charge),
                            parent_sales_quotations_id = COALESCE(p_parent_sales_quotations_id, parent_sales_quotations_id)
                        WHERE id = p_id AND is_active = true
                        RETURNING 1 INTO updated_rows;

                        IF updated_rows = 0 THEN
                            RAISE EXCEPTION 'No rows were updated for quotation ID %', p_id;
                        END IF;

                        RETURN true;
                    END;
                    $$ LANGUAGE plpgsql;
                ";
                await connection.ExecuteAsync(updateQuotationSql);

                Console.WriteLine("Successfully created/updated stored procedures.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating stored procedures: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                throw;
            }
        }

        public override async Task<SalesQuotation?> GetByIdAsync(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
                throw new ArgumentException("Invalid quotation ID", nameof(id));

            try
            {
                using var connection = CreateConnection();
                var quotation = await connection.QuerySingleOrDefaultAsync<SalesQuotation>(
                    "SELECT * FROM get_quotation_by_id(@p_id);",
                    new { p_id = id.Value });

                return quotation;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving quotation with ID {id}: {ex.Message}", ex);
            }
        }

        public override async Task<IEnumerable<SalesQuotation>> GetAllAsync()
        {
            try
            {
                using var connection = CreateConnection();
                var quotations = await connection.QueryAsync<SalesQuotation>(
                    "SELECT * FROM get_all_quotations();");

                return quotations ?? Enumerable.Empty<SalesQuotation>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving quotations: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<SalesQuotation>> GetQuotationsByOpportunityIdAsync(string opportunityId)
        {
            if (string.IsNullOrEmpty(opportunityId))
                throw new ArgumentException("OpportunityId cannot be null or empty", nameof(opportunityId));

            if (!int.TryParse(opportunityId, out int oppId))
                throw new ArgumentException("Invalid opportunity ID format", nameof(opportunityId));

            try
            {
                using var connection = CreateConnection();
                var quotations = await connection.QueryAsync<SalesQuotation>(
                    "SELECT * FROM get_quotations_by_opportunity(@p_opportunity_id);",
                    new { p_opportunity_id = oppId });

                return quotations ?? Enumerable.Empty<SalesQuotation>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving quotations for opportunity {opportunityId}: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<SalesQuotation>> GetQuotationsByCustomerIdAsync(string customerId)
        {
            if (string.IsNullOrEmpty(customerId))
                throw new ArgumentException("CustomerId cannot be null or empty", nameof(customerId));

            if (!int.TryParse(customerId, out int custId))
                throw new ArgumentException("Invalid customer ID format", nameof(customerId));

            try
            {
                using var connection = CreateConnection();
                var quotations = await connection.QueryAsync<SalesQuotation>(
                    "SELECT * FROM get_quotations_by_customer(@p_customer_id);",
                    new { p_customer_id = custId });

                return quotations ?? Enumerable.Empty<SalesQuotation>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving quotations for customer {customerId}: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<SalesQuotation>> GetQuotationsByLeadIdAsync(string leadId)
        {
            if (string.IsNullOrEmpty(leadId))
                throw new ArgumentException("LeadId cannot be null or empty", nameof(leadId));

            if (!int.TryParse(leadId, out int lId))
                throw new ArgumentException("Invalid lead ID format", nameof(leadId));

            try
            {
                using var connection = CreateConnection();
                var quotations = await connection.QueryAsync<SalesQuotation>(
                    "SELECT * FROM sales_quotations WHERE lead_id = @p_lead_id AND is_active = true ORDER BY date_created DESC;",
                    new { p_lead_id = lId });

                return quotations ?? Enumerable.Empty<SalesQuotation>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving quotations for lead {leadId}: {ex.Message}", ex);
            }
        }

        public override async Task<int> CreateAsync(SalesQuotation quotation)
        {
            if (quotation == null)
                throw new ArgumentNullException(nameof(quotation));

            ValidateQuotation(quotation);

            // Validate user exists
            if (!await ValidateUserExists(quotation.UserCreated))
                throw new ArgumentException($"User with ID {quotation.UserCreated} does not exist", nameof(quotation.UserCreated));

            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();                var parameters = new
                {                    p_user_created = quotation.UserCreated.GetValueOrDefault(1),
                    p_version = quotation.Version,
                    p_terms = quotation.Terms,
                    p_valid_till = quotation.ValidTill ?? DateTime.UtcNow.AddDays(30),
                    p_quotation_for = quotation.QuotationFor,
                    p_status = quotation.Status ?? "Draft",
                    p_lost_reason = quotation.LostReason,
                    p_customer_id = quotation.CustomerId > 0 ? quotation.CustomerId : 0,
                    p_quotation_type = quotation.QuotationType,
                    p_quotation_date = quotation.QuotationDate ?? DateTime.UtcNow,
                    p_order_type = quotation.OrderType,
                    p_comments = quotation.Comments,
                    p_delivery_within = quotation.DeliveryWithin,
                    p_delivery_after = quotation.DeliveryPrepareAfter,
                    p_quotation_id = quotation.QuotationId,                    p_opportunity_id = quotation.OpportunityId > 0 ? quotation.OpportunityId : 0,
                    p_customer_name = quotation.CustomerName,
                    p_taxes = quotation.Taxes,
                    p_delivery = quotation.Delivery,
                    p_payment = quotation.Payment,
                    p_warranty = quotation.Warranty,
                    p_freight_charge = quotation.FreightCharge,
p_parent_sales_quotations_id = (quotation.ParentSalesQuotationsId.HasValue && quotation.ParentSalesQuotationsId.Value > 0)
    ? quotation.ParentSalesQuotationsId.Value
    : (object)DBNull.Value                };                var sql = @"SELECT create_quotation(
                    @p_user_created, @p_version, @p_terms, @p_valid_till, @p_quotation_for,
                    @p_status, @p_lost_reason, @p_customer_id, @p_quotation_type, @p_quotation_date,
                    @p_order_type, @p_comments, @p_delivery_within, @p_delivery_after, @p_quotation_id,
                    @p_opportunity_id, @p_customer_name, @p_taxes, @p_delivery, @p_payment, @p_warranty,
                    @p_freight_charge, @p_parent_sales_quotations_id
                );";
                var id = await connection.QuerySingleAsync<int>(sql, parameters);

                if (id <= 0)
                    throw new Exception("Failed to create quotation - no ID returned");

                return id;
            }
            catch (PostgresException pgEx)
            {
                throw new Exception($"Database error creating quotation: {pgEx.Message}", pgEx);
            }
            catch (Exception ex)
            {
                // If it's our validation exception, throw it as is
                if (ex is ArgumentException)
                    throw;
                throw new Exception($"Error creating quotation: {ex.Message}", ex);
            }
        }        public override async Task<bool> UpdateAsync(SalesQuotation quotation)
        {
            if (quotation == null)
                throw new ArgumentNullException(nameof(quotation));

            if (!quotation.Id.HasValue)
                throw new ArgumentException("Quotation ID is required for update", nameof(quotation));

            ValidateQuotation(quotation);

            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                // First check the quotation's existence and status directly
                var quotationStatus = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT id, is_active 
                    FROM sales_quotations 
                    WHERE id = @Id", 
                    new { Id = quotation.Id.Value });

                if (quotationStatus == null)
                {
                    throw new Exception($"Quotation with ID {quotation.Id.Value} does not exist");
                }

                if (!quotationStatus.is_active)
                {
                    throw new Exception($"Quotation with ID {quotation.Id.Value} is not active");
                }

                // Validate user exists
                if (!await ValidateUserExists(quotation.UserUpdated))
                    throw new ArgumentException($"User with ID {quotation.UserUpdated} does not exist", nameof(quotation.UserUpdated));

                var parameters = CreateParametersFromQuotation(quotation, quotation.CustomerId, quotation.OpportunityId);
                parameters.Add("p_id", quotation.Id.Value);

                // Log the update attempt
                Console.WriteLine($"Attempting to update quotation ID {quotation.Id.Value}");
                Console.WriteLine($"Status check result - Exists: true, Is Active: {quotationStatus.is_active}");

                // Do a direct update instead of using the stored procedure
                var updateSql = @"
                    UPDATE sales_quotations 
                    SET 
                        user_updated = @p_user_updated,
                        date_updated = CURRENT_TIMESTAMP,
                        version = COALESCE(NULLIF(@p_version, ''), version),
                        terms = COALESCE(NULLIF(@p_terms, ''), terms),
                        valid_till = COALESCE(@p_valid_till, valid_till),
                        quotation_for = COALESCE(NULLIF(@p_quotation_for, ''), quotation_for),
                        status = COALESCE(NULLIF(@p_status, ''), status),
                        lost_reason = COALESCE(NULLIF(@p_lost_reason, ''), lost_reason),
                        customer_id = COALESCE(@p_customer_id, customer_id),
                        quotation_type = COALESCE(NULLIF(@p_quotation_type, ''), quotation_type),
                        quotation_date = COALESCE(@p_quotation_date, quotation_date),
                        order_type = COALESCE(NULLIF(@p_order_type, ''), order_type),
                        comments = COALESCE(NULLIF(@p_comments, ''), comments),
                        delivery_within = COALESCE(NULLIF(@p_delivery_within, ''), delivery_within),
                        delivery_after = COALESCE(NULLIF(@p_delivery_after, ''), delivery_after),
                        quotation_id = COALESCE(NULLIF(@p_quotation_id, ''), quotation_id),
                        opportunity_id = COALESCE(@p_opportunity_id, opportunity_id),
                        customer_name = COALESCE(NULLIF(@p_customer_name, ''), customer_name),
                        taxes = COALESCE(NULLIF(@p_taxes, ''), taxes),
                        delivery = COALESCE(NULLIF(@p_delivery, ''), delivery),
                        payment = COALESCE(NULLIF(@p_payment, ''), payment),
                        warranty = COALESCE(NULLIF(@p_warranty, ''), warranty),
                        freight_charge = COALESCE(NULLIF(@p_freight_charge, ''), freight_charge),
                        parent_sales_quotations_id = COALESCE(@p_parent_sales_quotations_id, parent_sales_quotations_id)
                    WHERE id = @p_id AND is_active = true;";

                var rowsAffected = await connection.ExecuteAsync(updateSql, parameters);
                Console.WriteLine($"Update result: {rowsAffected} rows affected");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                // If it's our validation exception, throw it as is
                if (ex is ArgumentException)
                    throw;
                throw new Exception($"Error updating quotation: {ex.Message}", ex);
            }
        }

        public override async Task<bool> DeleteAsync(int id)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();
                try
                {
                    var result = await connection.ExecuteScalarAsync<bool>(
                        "SELECT delete_quotation(@p_id, @p_user_updated);",
                        new { p_id = id, p_user_updated = (int?)null },
                        transaction);

                    await transaction.CommitAsync();
                    return result;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting quotation {id}: {ex.Message}", ex);
            }
        }

        protected override string GenerateInsertQuery()
        {
            return @"SELECT create_quotation(
                @UserCreated,
                @Version,
                @Terms,
                @ValidTill,
                @QuotationFor,
                @Status,
                @LostReason,
                @CustomerId,
                @QuotationType,
                @QuotationDate,
                @OrderType,
                @Comments,
                @DeliveryWithin,
                @DeliveryPrepareAfter,
                @QuotationId,
                @OpportunityId,
                @CustomerName,
                @Taxes,
                @Delivery,
                @Payment,
                @Warranty,
                @FreightCharge,
                @ParentSalesQuotationsId
            );";
        }

        protected override string GenerateUpdateQuery()
        {
            return @"
                SELECT update_quotation(
                    @p_id,
                    @p_user_updated,
                    @p_version,
                    @p_terms,
                    @p_valid_till,
                    @p_quotation_for,
                    @p_status,
                    @p_lost_reason,
                    @p_customer_id,
                    @p_quotation_type,
                    @p_quotation_date,
                    @p_order_type,
                    @p_comments,
                    @p_delivery_within,
                    @p_delivery_after,
                    @p_quotation_id,
                    @p_opportunity_id,
                    @p_customer_name,
                    @p_taxes,
                    @p_delivery,
                    @p_payment,
                    @p_warranty,
                    @p_freight_charge,
                    @p_parent_sales_quotations_id
                );";
        }        private void ValidateQuotation(SalesQuotation quotation)
        {
            // All fields are optional now
            // Provide default values if needed
            if (string.IsNullOrEmpty(quotation.Version))
                quotation.Version = "1.0";

            if (string.IsNullOrEmpty(quotation.Status))
                quotation.Status = "Draft";

            if (!quotation.QuotationDate.HasValue)
                quotation.QuotationDate = DateTime.UtcNow;
        }

        private DynamicParameters CreateParametersFromQuotation(SalesQuotation quotation, int? customerId = null, int? opportunityId = null)
        {
            var parameters = new DynamicParameters();
            
            // Handle nullable int parameters with DBNull
            parameters.Add("p_user_updated", quotation.UserUpdated.HasValue ? (object)quotation.UserUpdated.Value : DBNull.Value);
            parameters.Add("p_version", (object)(quotation.Version?.Trim() ?? string.Empty));
            parameters.Add("p_terms", (object)(quotation.Terms?.Trim() ?? string.Empty));
            parameters.Add("p_valid_till", (object)(quotation.ValidTill ?? DateTime.UtcNow.AddMonths(1)));
            parameters.Add("p_quotation_for", (object)(quotation.QuotationFor?.Trim() ?? string.Empty));
            parameters.Add("p_status", (object)(quotation.Status?.Trim() ?? string.Empty));
            parameters.Add("p_lost_reason", (object)(quotation.LostReason?.Trim() ?? string.Empty));
            parameters.Add("p_customer_id", customerId.HasValue ? (object)customerId.Value : DBNull.Value);
            parameters.Add("p_quotation_type", (object)(quotation.QuotationType?.Trim() ?? string.Empty));
            parameters.Add("p_quotation_date", (object)(quotation.QuotationDate ?? DateTime.UtcNow));
            parameters.Add("p_order_type", (object)(quotation.OrderType?.Trim() ?? string.Empty));
            parameters.Add("p_comments", (object)(quotation.Comments?.Trim() ?? string.Empty));
            parameters.Add("p_delivery_within", (object)(quotation.DeliveryWithin?.Trim() ?? string.Empty));
            parameters.Add("p_delivery_after", (object)(quotation.DeliveryPrepareAfter?.Trim() ?? string.Empty));
            parameters.Add("p_quotation_id", (object)(quotation.QuotationId?.Trim() ?? string.Empty));
            parameters.Add("p_opportunity_id", opportunityId.HasValue ? (object)opportunityId.Value : DBNull.Value);
            parameters.Add("p_customer_name", (object)(quotation.CustomerName?.Trim() ?? string.Empty));
            parameters.Add("p_taxes", (object)(quotation.Taxes?.Trim() ?? string.Empty));
            parameters.Add("p_delivery", (object)(quotation.Delivery?.Trim() ?? string.Empty));
            parameters.Add("p_payment", (object)(quotation.Payment?.Trim() ?? string.Empty));
            parameters.Add("p_warranty", (object)(quotation.Warranty?.Trim() ?? string.Empty));
            parameters.Add("p_freight_charge", (object)(quotation.FreightCharge?.Trim() ?? string.Empty));
            parameters.Add("p_parent_sales_quotations_id", quotation.ParentSalesQuotationsId.HasValue ? (object)quotation.ParentSalesQuotationsId.Value : DBNull.Value);

            return parameters;
        }

        private async Task<bool> ValidateUserExists(int? userId)
        {
            if (!userId.HasValue)
                return false;

            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var exists = await connection.QuerySingleOrDefaultAsync<bool>(
                    "SELECT EXISTS(SELECT 1 FROM users WHERE user_id = @UserId)",
                    new { UserId = userId });

                return exists;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}