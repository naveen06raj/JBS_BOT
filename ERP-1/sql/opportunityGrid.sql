CREATE OR REPLACE FUNCTION fn_get_opportunities_with_pagination(
    p_search_text TEXT DEFAULT NULL,
    p_customer_names TEXT[] DEFAULT NULL,
    p_territories TEXT[] DEFAULT NULL,
    p_statuses TEXT[] DEFAULT NULL,
    p_stages TEXT[] DEFAULT NULL,
    p_opportunity_types TEXT[] DEFAULT NULL,
    p_page_number INTEGER DEFAULT 1,
    p_page_size INTEGER DEFAULT 10,
    p_order_by TEXT DEFAULT 'date_created',
    p_order_direction TEXT DEFAULT 'DESC'
)
RETURNS TABLE (
    "TotalRecords" INTEGER,
    "Id" INTEGER,
    "UserCreated" INTEGER,
    "DateCreated" TIMESTAMP,
    "UserUpdated" INTEGER,
    "DateUpdated" TIMESTAMP,
    "OpportunityId" VARCHAR,
    "CustomerName" VARCHAR,
    "ContactName" VARCHAR,
    "ContactEmail" VARCHAR,
    "ContactPhone" VARCHAR,
    "Status" VARCHAR,
    "Stage" VARCHAR,
    "EstimatedValue" DECIMAL,
    "ActualValue" DECIMAL,
    "ExpectedClosingDate" TIMESTAMP,
    "OpportunityType" VARCHAR,
    "BusinessChallenge" TEXT,
    "IsActive" BOOLEAN,
    "Comments" TEXT,
    "TerritoryId" INTEGER,
    "TerritoryName" VARCHAR,
    "CityName" VARCHAR,
    "StateName" VARCHAR,
    "ProbabilityOfWinning" VARCHAR,
    "CompetitorName" VARCHAR,
    "SalesRepresentative" VARCHAR
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_offset INTEGER;
    v_where_clause TEXT := ' WHERE 1=1 ';
    v_total_records INTEGER;
BEGIN
    -- Calculate offset
    v_offset := (p_page_number - 1) * p_page_size;

    -- Build dynamic WHERE clause
    IF p_search_text IS NOT NULL AND p_search_text != '' THEN
        v_where_clause := v_where_clause ||        ' AND (            LOWER(so.customer_name) LIKE LOWER(''%'' || $1 || ''%'') OR
            LOWER(so.opportunity_id) LIKE LOWER(''%'' || $1 || ''%'') OR
            LOWER(so.contact_name) LIKE LOWER(''%'' || $1 || ''%'') OR
            LOWER(so.contact_mobile_no) LIKE LOWER(''%'' || $1 || ''%'')
        )';
    END IF;

    IF p_customer_names IS NOT NULL AND array_length(p_customer_names, 1) > 0 THEN
        v_where_clause := v_where_clause || ' AND so.customer_name = ANY($2)';
    END IF;    IF p_territories IS NOT NULL AND array_length(p_territories, 1) > 0 THEN
        v_where_clause := v_where_clause || ' AND so.territory_id::text = ANY($3)';
    END IF;

    IF p_statuses IS NOT NULL AND array_length(p_statuses, 1) > 0 THEN
        v_where_clause := v_where_clause || ' AND so.status = ANY($4)';
    END IF;    IF p_opportunity_types IS NOT NULL AND array_length(p_opportunity_types, 1) > 0 THEN
        v_where_clause := v_where_clause || ' AND so.opportunity_type = ANY($5)';
    END IF;

    -- Always filter for isactive = true
    v_where_clause := v_where_clause || ' AND so.isactive = true';

    -- Get total records
    EXECUTE 'SELECT COUNT(*) FROM sales_opportunities so'
             || v_where_clause    INTO v_total_records
    USING p_search_text, p_customer_names, p_territories, p_statuses, p_opportunity_types;

    -- Return paginated results
    RETURN QUERY EXECUTE 
    'WITH base_query AS (
        SELECT 
            so.id AS "Id",
            so.user_created AS "UserCreated",
            so.date_created AS "DateCreated",
            so.user_updated AS "UserUpdated",
            so.date_updated AS "DateUpdated",
            so.opportunity_id AS "OpportunityId",
            so.customer_name AS "CustomerName",
            so.contact_name AS "ContactName",
            so.contact_mobile_no AS "ContactEmail",
            so.contact_mobile_no AS "ContactPhone",
            so.status AS "Status",
            so.status AS "Stage",
            0::DECIMAL AS "EstimatedValue",
            0::DECIMAL AS "ActualValue",
            so.expected_completion::TIMESTAMP AS "ExpectedClosingDate",
            so.opportunity_type AS "OpportunityType",
            NULL::TEXT AS "BusinessChallenge",
            so.isactive AS "IsActive",
            so.comments AS "Comments",
            NULL::INTEGER AS "TerritoryId",
            NULL::VARCHAR AS "TerritoryName",
            NULL::VARCHAR AS "CityName",
            NULL::VARCHAR AS "StateName",
            NULL::VARCHAR AS "ProbabilityOfWinning",
            NULL::VARCHAR AS "CompetitorName",
            so.sales_representative_id::VARCHAR AS "SalesRepresentative"
        FROM sales_opportunities so'
        || v_where_clause || 
        ' ORDER BY so.' || quote_ident(p_order_by) || ' ' || p_order_direction || 
        ' LIMIT ' || p_page_size || ' OFFSET ' || v_offset || '
    )
    SELECT 
        ' || v_total_records || ' AS "TotalRecords",
        bq.*
    FROM base_query bq'
    USING p_search_text, p_customer_names, p_territories, p_statuses, p_stages, p_opportunity_types;
END;
$$;
