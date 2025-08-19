DROP FUNCTION IF EXISTS fn_get_sales_deals_grid;

CREATE OR REPLACE FUNCTION fn_get_sales_deals_grid(
    IN p_request json,
    OUT p_deals json,
    OUT p_total_records integer
)
RETURNS RECORD
LANGUAGE plpgsql
AS $function$
DECLARE
    v_search_text TEXT;
    v_customer_names TEXT[];
    v_statuses TEXT[];
    v_deal_ids TEXT[];
    v_page_number INTEGER;
    v_page_size INTEGER;
    v_offset INTEGER;
    v_where_clause TEXT;
    v_base_query TEXT;
    v_order_by TEXT;
    v_order_direction TEXT;
    v_sql TEXT;
BEGIN
    -- Extract parameters from JSON
    v_search_text := p_request->>'searchText';
    v_customer_names := ARRAY(SELECT jsonb_array_elements_text(CAST(p_request AS jsonb)->'customerNames'));
    v_statuses := ARRAY(SELECT jsonb_array_elements_text(CAST(p_request AS jsonb)->'statuses'));
    v_deal_ids := ARRAY(SELECT jsonb_array_elements_text(CAST(p_request AS jsonb)->'dealIds'));
    v_page_number := COALESCE((p_request->>'pageNumber')::INTEGER, 1);
    v_page_size := COALESCE((p_request->>'pageSize')::INTEGER, 10);
    v_order_by := COALESCE(p_request->>'orderBy', 'date_created');
    v_order_direction := COALESCE(p_request->>'orderDirection', 'DESC');

    -- Calculate offset for pagination
    v_offset := (v_page_number - 1) * v_page_size;

    -- Initialize WHERE clause
    v_where_clause := 'WHERE sd.isactive = true';

    -- Add search filter with partial matching
    IF v_search_text IS NOT NULL AND v_search_text != '' AND v_search_text != 'string' THEN
        v_where_clause := v_where_clause || format(
            ' AND (
                LOWER(c.name) LIKE ''%%'' || LOWER(%L) || ''%%'' OR
                LOWER(sd.deal_name) LIKE ''%%'' || LOWER(%L) || ''%%'' OR
                LOWER(sd.status) LIKE ''%%'' || LOWER(%L) || ''%%'' OR
                LOWER(sd.deal_for) LIKE ''%%'' || LOWER(%L) || ''%%'' OR
                LOWER(sd.comments) LIKE ''%%'' || LOWER(%L) || ''%%''
            )',
            v_search_text, v_search_text, v_search_text, v_search_text, v_search_text
        );
    END IF;

    -- Add array filters with partial matching support
    IF array_length(v_customer_names, 1) > 0 AND NOT (array_length(v_customer_names, 1) = 1 AND v_customer_names[1] = 'string') THEN
        v_where_clause := v_where_clause || format(
            ' AND EXISTS (
                SELECT 1 FROM unnest(%L::varchar[]) AS cn 
                WHERE LOWER(c.name) LIKE ''%%'' || LOWER(cn) || ''%%''
            )',
            v_customer_names
        );
    END IF;

    IF array_length(v_statuses, 1) > 0 AND NOT (array_length(v_statuses, 1) = 1 AND v_statuses[1] = 'string') THEN
        v_where_clause := v_where_clause || format(
            ' AND EXISTS (
                SELECT 1 FROM unnest(%L::varchar[]) AS s 
                WHERE LOWER(sd.status) LIKE ''%%'' || LOWER(s) || ''%%''
            )',
            v_statuses
        );
    END IF;

    IF array_length(v_deal_ids, 1) > 0 AND NOT (array_length(v_deal_ids, 1) = 1 AND v_deal_ids[1] = 'string') THEN
        v_where_clause := v_where_clause || format(
            ' AND EXISTS (
                SELECT 1 FROM unnest(%L::varchar[]) AS did 
                WHERE sd.id::text LIKE ''%%'' || did || ''%%''
            )',
            v_deal_ids
        );
    END IF;

    -- Base query for getting deal data
    v_base_query := '
        FROM sales_deals sd
        LEFT JOIN sales_customers c ON sd.customer_id = c.id
        LEFT JOIN sales_opportunities o ON sd.opportunity_id = o.id
        LEFT JOIN sales_employees se ON sd.sales_representative_id = se.id
        LEFT JOIN sales_employees uc ON sd.user_created = uc.id
        LEFT JOIN sales_employees uu ON sd.user_updated = uu.id
    ' || v_where_clause;

    -- Prepare ORDER BY clause
    v_order_by := CASE lower(v_order_by)
        WHEN 'date_created' THEN 'sd.date_created'
        WHEN 'deal_name' THEN 'sd.deal_name'
        WHEN 'customer_name' THEN 'c.name'
        WHEN 'status' THEN 'sd.status'
        WHEN 'amount' THEN 'sd.amount'
        WHEN 'close_date' THEN 'sd.close_date'
        ELSE 'sd.date_created'
    END;
    
    v_order_direction := CASE upper(v_order_direction)
        WHEN 'ASC' THEN 'ASC'
        ELSE 'DESC'
    END;

    -- Construct and execute the main query
    v_sql := format('
        WITH pagination_data AS (
            SELECT count(*) OVER() as full_count,
                sd.id,
                sd.deal_name,
                sd.amount,
                sd.expected_revenue,
                sd.start_date,
                sd.close_date,
                sd.status,
                sd.deal_for,
                sd.comments,
                c.name as customer_name,
                o.opportunity_name as opportunity_name,
                se.name as sales_representative,
                uc.name as created_by,
                uu.name as updated_by,
                sd.date_created,
                sd.date_updated
            %s
            ORDER BY %s %s
            LIMIT %s OFFSET %s
        )
        SELECT 
            (
                SELECT COALESCE(json_agg(t), ''[]''::json)
                FROM (
                    SELECT 
                        id,
                        deal_name,
                        amount,
                        expected_revenue,
                        start_date,
                        close_date,
                        status,
                        deal_for,
                        comments,
                        customer_name,
                        opportunity_name,
                        sales_representative,
                        created_by,
                        updated_by,
                        date_created,
                        date_updated
                    FROM pagination_data
                ) t
            ) as deals,
            COALESCE((SELECT full_count FROM pagination_data LIMIT 1), 0) as total_records',
        v_base_query, v_order_by, v_order_direction, v_page_size, v_offset
    );    -- Execute the query and store results in OUT parameters
    EXECUTE v_sql INTO p_deals, p_total_records;
END;
$function$;
