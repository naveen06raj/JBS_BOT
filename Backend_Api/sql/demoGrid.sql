-- Drop existing functions if they exist
DROP FUNCTION IF EXISTS public.fn_get_demos_grid;
DROP FUNCTION IF EXISTS public.fn_get_sales_demos_grid(jsonb);

CREATE OR REPLACE FUNCTION public.fn_get_sales_demos_grid(
    p_request jsonb,
    OUT total_records INTEGER,
    OUT id INTEGER,
    OUT user_created INTEGER,
    OUT date_created TIMESTAMP WITH TIME ZONE,
    OUT user_updated INTEGER,
    OUT date_updated TIMESTAMP WITH TIME ZONE,
    OUT user_id INTEGER,
    OUT demo_date_time TIMESTAMP WITH TIME ZONE,
    OUT status VARCHAR,
    OUT customer_name VARCHAR,
    OUT demo_name VARCHAR,
    OUT demo_contact VARCHAR,
    OUT demo_approach VARCHAR,
    OUT demo_outcome VARCHAR,
    OUT demo_feedback VARCHAR,
    OUT comments VARCHAR,
    OUT opportunity_id INTEGER,
    OUT presenter_id INTEGER,
    OUT presenter_name TEXT,
    OUT address_id INTEGER,
    OUT customer_id INTEGER,
    OUT opportunity_name VARCHAR,
    OUT address_details TEXT,
    OUT user_created_name TEXT,
    OUT user_updated_name TEXT
)
RETURNS SETOF RECORD
SECURITY DEFINER
LANGUAGE plpgsql
AS $$
DECLARE
    v_page_size INTEGER;
    v_offset INTEGER;
    v_search_text VARCHAR;
    v_customer_names VARCHAR[];
    v_statuses VARCHAR[];
    v_demo_approaches VARCHAR[];
    v_demo_outcomes VARCHAR[];
    v_start_date TIMESTAMP;
    v_end_date TIMESTAMP;
    v_page_number INTEGER;
    v_order_by TEXT;
    v_order_direction TEXT;
    v_query TEXT;
    v_base_query TEXT;
    v_where_clause TEXT;
    v_total_records INTEGER;
BEGIN
    -- Log the input parameters
    RAISE NOTICE 'Input request: %', p_request;
    -- Extract values from JSON
    v_search_text := NULLIF(p_request->>'searchText', 'string');
    v_customer_names := ARRAY(SELECT jsonb_array_elements_text(COALESCE(p_request->'customerNames', '[]'::jsonb)));
    v_statuses := ARRAY(SELECT jsonb_array_elements_text(COALESCE(p_request->'statuses', '[]'::jsonb)));
    v_demo_approaches := ARRAY(SELECT jsonb_array_elements_text(COALESCE(p_request->'demoApproaches', '[]'::jsonb)));
    v_demo_outcomes := ARRAY(SELECT jsonb_array_elements_text(COALESCE(p_request->'demoOutcomes', '[]'::jsonb)));
    v_start_date := (p_request->>'startDate')::TIMESTAMP;
    v_end_date := (p_request->>'endDate')::TIMESTAMP;
    v_page_number := COALESCE((p_request->>'pageNumber')::INTEGER, 1);
    v_page_size := LEAST(COALESCE((p_request->>'pageSize')::INTEGER, 10), 1000);
    v_order_by := COALESCE(p_request->>'orderBy', 'date_created');
    v_order_direction := COALESCE(p_request->>'orderDirection', 'DESC');
      
    -- Calculate offset for pagination
    v_offset := (v_page_number - 1) * v_page_size;

    -- Initialize WHERE clause
    v_where_clause := 'WHERE 1=1';
    
    -- Log the initial parameters
    RAISE NOTICE 'Search parameters: searchText=%, customerNames=%, statuses=%, pageNumber=%, pageSize=%', 
        v_search_text, v_customer_names, v_statuses, v_page_number, v_page_size;

    -- Add search filters
    IF v_search_text IS NOT NULL AND v_search_text != '' THEN
        v_where_clause := v_where_clause || format('
            AND (
                LOWER(sd.demo_name) LIKE LOWER(''%%''||%L||''%%'') OR 
                LOWER(sd.customer_name) LIKE LOWER(''%%''||%L||''%%'') OR
                LOWER(sd.demo_contact) LIKE LOWER(''%%''||%L||''%%'') OR
                LOWER(so.opportunity_name) LIKE LOWER(''%%''||%L||''%%'')
            )',
            v_search_text, v_search_text, v_search_text, v_search_text
        );
    END IF;

    -- Add array filters with proper handling of 'string' value
    IF array_length(v_customer_names, 1) > 0 AND NOT (array_length(v_customer_names, 1) = 1 AND v_customer_names[1] = 'string') THEN
        v_where_clause := v_where_clause || format('
            AND EXISTS (
                SELECT 1 FROM unnest(%L::varchar[]) AS cn 
                WHERE LOWER(sd.customer_name) LIKE LOWER(''%%'' || cn || ''%%'')
            )',
            v_customer_names
        );
    END IF;

    IF array_length(v_statuses, 1) > 0 AND NOT (array_length(v_statuses, 1) = 1 AND v_statuses[1] = 'string') THEN
        v_where_clause := v_where_clause || format('
            AND EXISTS (
                SELECT 1 FROM unnest(%L::varchar[]) AS s 
                WHERE LOWER(sd.status) LIKE LOWER(''%%'' || s || ''%%'')
            )',
            v_statuses
        );
    END IF;

    IF array_length(v_demo_approaches, 1) > 0 AND NOT (array_length(v_demo_approaches, 1) = 1 AND v_demo_approaches[1] = 'string') THEN
        v_where_clause := v_where_clause || format('
            AND EXISTS (
                SELECT 1 FROM unnest(%L::varchar[]) AS da 
                WHERE LOWER(sd.demo_approach) LIKE LOWER(''%%'' || da || ''%%'')
            )',
            v_demo_approaches
        );
    END IF;

    IF array_length(v_demo_outcomes, 1) > 0 AND NOT (array_length(v_demo_outcomes, 1) = 1 AND v_demo_outcomes[1] = 'string') THEN
        v_where_clause := v_where_clause || format('
            AND EXISTS (
                SELECT 1 FROM unnest(%L::varchar[]) AS do 
                WHERE LOWER(sd.demo_outcome) LIKE LOWER(''%%'' || do || ''%%'')
            )',
            v_demo_outcomes
        );
    END IF;

    -- Add date range filter
    IF v_start_date IS NOT NULL THEN
        v_where_clause := v_where_clause || format('
            AND sd.demo_date >= %L::timestamp',
            v_start_date
        );
    END IF;

    IF v_end_date IS NOT NULL THEN
        v_where_clause := v_where_clause || format('
            AND sd.demo_date <= %L::timestamp',
            v_end_date
        );
    END IF;

    -- Base query for getting demo data
    v_base_query := '
        FROM public.sales_demos sd
        LEFT JOIN public.users u_presenter ON sd.presenter_id = u_presenter.user_id
        LEFT JOIN public.users u_created ON sd.user_created = u_created.user_id
        LEFT JOIN public.users u_updated ON sd.user_updated = u_updated.user_id
        LEFT JOIN public.sales_opportunities so ON sd.opportunity_id = so.id
        LEFT JOIN public.sales_addresses sa ON sd.address_id = sa.id
    ' || v_where_clause;

    -- Prepare ORDER BY clause
    v_order_by := CASE lower(v_order_by)
        WHEN 'date_created' THEN 'sd.date_created'
        WHEN 'demo_date' THEN 'sd.demo_date'
        WHEN 'customer_name' THEN 'sd.customer_name'
        WHEN 'status' THEN 'sd.status'
        WHEN 'demo_name' THEN 'sd.demo_name'
        ELSE 'sd.date_created'
    END;
    
    v_order_direction := CASE upper(v_order_direction)
        WHEN 'ASC' THEN 'ASC'
        ELSE 'DESC'
    END;

    -- Get total record count
    EXECUTE format('SELECT COUNT(*) %s', v_base_query)
    INTO v_total_records;

    -- Return main query results
    RETURN QUERY EXECUTE format('
        SELECT 
            %s AS total_records,
            sd.id,
            sd.user_created,
            sd.date_created AT TIME ZONE ''UTC'',
            sd.user_updated,
            sd.date_updated AT TIME ZONE ''UTC'',
            sd.user_id,
            sd.demo_date AT TIME ZONE ''UTC'' as demo_date_time,
            sd.status,
            sd.customer_name,
            sd.demo_name,
            sd.demo_contact,
            sd.demo_approach,
            sd.demo_outcome,
            sd.demo_feedback,
            sd.comments,
            sd.opportunity_id,
            sd.presenter_id,
            CONCAT(u_presenter.first_name, '' '', u_presenter.last_name) as presenter_name,
            sd.address_id,
            sd.customer_id,
            so.opportunity_name,
            CONCAT_WS('', '', 
                NULLIF(sa.door_no, ''''),
                NULLIF(sa.street, ''''),
                NULLIF(sa.area, ''''),
                NULLIF(sa.city, ''''),
                NULLIF(sa.state, ''''),
                NULLIF(sa.pincode, '''')
            ) as address_details,
            CONCAT(u_created.first_name, '' '', u_created.last_name) as user_created_name,
            CONCAT(u_updated.first_name, '' '', u_updated.last_name) as user_updated_name
        %s
        ORDER BY %s %s
        LIMIT %s 
        OFFSET %s',
        v_total_records,
        v_base_query,
        v_order_by,
        v_order_direction,
        v_page_size,
        v_offset
    );
END;
$$;
