-- First, make sure the sales_demos table exists with the correct structure
CREATE TABLE IF NOT EXISTS public.sales_demos (
    id SERIAL PRIMARY KEY,
    user_created INTEGER,
    date_created TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    user_updated INTEGER,
    date_updated TIMESTAMP WITH TIME ZONE,
    user_id INTEGER,
    demo_date TIMESTAMP WITH TIME ZONE,
    status VARCHAR(100),
    customer_name VARCHAR(255),
    demo_name VARCHAR(255),
    demo_contact VARCHAR(255),
    demo_approach VARCHAR(255),
    demo_outcome VARCHAR(255),
    demo_feedback VARCHAR(255),
    comments VARCHAR(255),
    opportunity_id INTEGER,
    presenter_id INTEGER,
    address_id INTEGER,
    customer_id INTEGER
);

-- Drop and recreate the function
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
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
DECLARE
    v_page_size INTEGER;
    v_offset INTEGER;
    v_search_text TEXT;
    v_customer_names TEXT[];
    v_statuses TEXT[];
    v_demo_approaches TEXT[];
    v_demo_outcomes TEXT[];
    v_page_number INTEGER;
    v_order_by TEXT;
    v_order_direction TEXT;
    v_where_clause TEXT := 'WHERE 1=1';
    v_base_query TEXT;
BEGIN
    -- Parse basic values
    v_search_text := NULLIF(TRIM(p_request->>'searchText'), '')::TEXT;
    v_page_number := COALESCE((p_request->>'pageNumber')::INTEGER, 1);
    v_page_size := LEAST(COALESCE((p_request->>'pageSize')::INTEGER, 10), 1000);
    v_order_by := COALESCE(NULLIF(p_request->>'orderBy', ''), 'date_created');
    v_order_direction := UPPER(COALESCE(NULLIF(p_request->>'orderDirection', ''), 'DESC'));

    v_offset := (v_page_number - 1) * v_page_size;

    -- Safely parse arrays
    v_customer_names := ARRAY(
        SELECT value::text
        FROM jsonb_array_elements_text(CASE WHEN p_request->'customerNames' IS NULL THEN '[]'::jsonb ELSE p_request->'customerNames' END) AS value
        WHERE value <> 'string'
    );

    v_statuses := ARRAY(
        SELECT value::text
        FROM jsonb_array_elements_text(CASE WHEN p_request->'statuses' IS NULL THEN '[]'::jsonb ELSE p_request->'statuses' END) AS value
        WHERE value <> 'string'
    );

    v_demo_approaches := ARRAY(
        SELECT value::text
        FROM jsonb_array_elements_text(CASE WHEN p_request->'demoApproaches' IS NULL THEN '[]'::jsonb ELSE p_request->'demoApproaches' END) AS value
        WHERE value <> 'string'
    );

    v_demo_outcomes := ARRAY(
        SELECT value::text
        FROM jsonb_array_elements_text(CASE WHEN p_request->'demoOutcomes' IS NULL THEN '[]'::jsonb ELSE p_request->'demoOutcomes' END) AS value
        WHERE value <> 'string'
    );

    -- Filters
    IF v_search_text IS NOT NULL THEN
        v_where_clause := v_where_clause || format('
            AND (
                LOWER(sd.demo_name) LIKE LOWER(''%%'' || %L || ''%%'')
                OR LOWER(sd.customer_name) LIKE LOWER(''%%'' || %L || ''%%'')
                OR LOWER(sd.demo_contact) LIKE LOWER(''%%'' || %L || ''%%'')
                OR LOWER(so.opportunity_name) LIKE LOWER(''%%'' || %L || ''%%'')
            )',
            v_search_text, v_search_text, v_search_text, v_search_text
        );
    END IF;

    -- Array filters using ANY
    IF array_length(v_customer_names, 1) > 0 THEN
        v_where_clause := v_where_clause || format('
            AND LOWER(sd.customer_name) LIKE ANY (SELECT LOWER(''%%'' || unnest || ''%%'') FROM unnest(%L))',
            v_customer_names
        );
    END IF;

    IF array_length(v_statuses, 1) > 0 THEN
        v_where_clause := v_where_clause || format('
            AND LOWER(sd.status) LIKE ANY (SELECT LOWER(''%%'' || unnest || ''%%'') FROM unnest(%L))',
            v_statuses
        );
    END IF;

    IF array_length(v_demo_approaches, 1) > 0 THEN
        v_where_clause := v_where_clause || format('
            AND LOWER(sd.demo_approach) LIKE ANY (SELECT LOWER(''%%'' || unnest || ''%%'') FROM unnest(%L))',
            v_demo_approaches
        );
    END IF;

    IF array_length(v_demo_outcomes, 1) > 0 THEN
        v_where_clause := v_where_clause || format('
            AND LOWER(sd.demo_outcome) LIKE ANY (SELECT LOWER(''%%'' || unnest || ''%%'') FROM unnest(%L))',
            v_demo_outcomes
        );
    END IF;

    -- Order by column
    v_order_by := CASE LOWER(v_order_by)
        WHEN 'demo_date' THEN 'sd.demo_date'
        WHEN 'customer_name' THEN 'sd.customer_name'
        WHEN 'status' THEN 'sd.status'
        WHEN 'demo_name' THEN 'sd.demo_name'
        ELSE 'sd.date_created'
    END;

    IF v_order_direction NOT IN ('ASC', 'DESC') THEN
        v_order_direction := 'DESC';
    END IF;

    -- Base query with joins
    v_base_query := format('
        FROM public.sales_demos sd
        LEFT JOIN public.users u_presenter ON sd.presenter_id = u_presenter.user_id
        LEFT JOIN public.users u_created ON sd.user_created = u_created.user_id
        LEFT JOIN public.users u_updated ON sd.user_updated = u_updated.user_id
        LEFT JOIN public.sales_opportunities so ON sd.opportunity_id = so.id
        LEFT JOIN public.sales_addresses sa ON sd.address_id = sa.id
        %s',
        v_where_clause
    );

    -- Get total records count
    EXECUTE format('SELECT COUNT(*) %s', v_base_query)
    INTO total_records;

    -- Return data
    RETURN QUERY EXECUTE format(
        'SELECT
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
        LIMIT %s OFFSET %s',
        total_records,
        v_base_query,
        v_order_by,
        v_order_direction,
        v_page_size,
        v_offset
    );
END;
$$;
