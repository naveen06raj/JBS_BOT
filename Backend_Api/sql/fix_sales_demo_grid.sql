-- Drop existing functions if they exist
DROP FUNC    -- Extract values from JSON, handling the specific request structure
    v_search_text := p_request->>'SearchText';
    v_customer_names := ARRAY(
        SELECT json_array_elements_text(COALESCE(p_request->'CustomerNames', '[]'::json))
        WHERE value != 'string' -- Filter out placeholder values
    );
    v_statuses := ARRAY(
        SELECT json_array_elements_text(COALESCE(p_request->'Statuses', '[]'::json))
        WHERE value != 'string' -- Filter out placeholder values
    );
    v_demo_approaches := ARRAY(
        SELECT json_array_elements_text(COALESCE(p_request->'DemoApproaches', '[]'::json))
        WHERE value != 'string' -- Filter out placeholder values
    );
    v_demo_outcomes := ARRAY(
        SELECT json_array_elements_text(COALESCE(p_request->'DemoOutcomes', '[]'::json))
        WHERE value != 'string' -- Filter out placeholder values
    );
    
    -- Parse dates with timezone
    v_start_date := (p_request->>'StartDate')::TIMESTAMP WITH TIME ZONE;
    v_end_date := (p_request->>'EndDate')::TIMESTAMP WITH TIME ZONE;
    
    -- Pagination and ordering
    v_page_number := COALESCE((p_request->>'PageNumber')::INTEGER, 1);
    v_page_size := LEAST(COALESCE((p_request->>'PageSize')::INTEGER, 10), 1000);
    v_order_by := COALESCE(p_request->>'OrderBy', 'DateCreated');
    v_order_direction := COALESCE(UPPER(p_request->>'OrderDirection'), 'DESC');TS public.fn_get_demos_grid;
DROP FUNCTION IF EXISTS public.fn_get_sales_demos_grid;

CREATE OR REPLACE FUNCTION public.fn_get_sales_demos_grid(
    p_request json
)
RETURNS TABLE (
    "TotalRecords" INTEGER,
    "Id" INTEGER,
    "UserCreated" INTEGER,
    "DateCreated" TIMESTAMP,
    "UserUpdated" INTEGER,
    "DateUpdated" TIMESTAMP,
    "UserId" INTEGER,
    "DemoDateTime" TIMESTAMP,
    "Status" VARCHAR(100),
    "CustomerName" VARCHAR(255),
    "DemoName" VARCHAR(255),
    "DemoContact" VARCHAR(255),
    "DemoApproach" VARCHAR(255),
    "DemoOutcome" VARCHAR(255),
    "DemoFeedback" VARCHAR(255),    
    "Comments" VARCHAR(255),
    "OpportunityId" INTEGER,
    "PresenterId" INTEGER,
    "PresenterName" TEXT,
    "AddressId" INTEGER,
    "CustomerId" INTEGER,
    "OpportunityName" VARCHAR(255),
    "AddressDetails" TEXT,
    "UserCreatedName" TEXT,
    "UserUpdatedName" TEXT
)
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
BEGIN
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
    v_order_by := COALESCE(p_request->>'orderBy', 'DateCreated');
    v_order_direction := COALESCE(p_request->>'orderDirection', 'DESC');
      
    -- Calculate offset for pagination
    v_offset := (v_page_number - 1) * v_page_size;

    -- Build main query
    v_query := '
        WITH base_data AS (
            SELECT 
                COUNT(*) OVER() as total_records,
                sd.id,
                sd.user_created,
                sd.date_created,
                sd.user_updated,
                sd.date_updated,
                sd.user_id,
                sd.demo_date as demo_date_time,
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
            FROM 
                public.sales_demos sd
                LEFT JOIN users u_presenter ON sd.presenter_id = u_presenter.user_id
                LEFT JOIN users u_created ON sd.user_created = u_created.user_id
                LEFT JOIN users u_updated ON sd.user_updated = u_updated.user_id
                LEFT JOIN sales_opportunities so ON sd.opportunity_id = so.id
                LEFT JOIN sales_addresses sa ON sd.address_id = sa.id
            WHERE 1=1
    ';

    -- Add search filters
    IF v_search_text IS NOT NULL AND v_search_text != '' THEN
        v_query := v_query || format('
            AND (
                LOWER(sd.demo_name) LIKE LOWER(''%%''||%L||''%%'') OR 
                LOWER(sd.customer_name) LIKE LOWER(''%%''||%L||''%%'') OR
                LOWER(sd.demo_contact) LIKE LOWER(''%%''||%L||''%%'') OR
                LOWER(so.opportunity_name) LIKE LOWER(''%%''||%L||''%%'')
            )',
            v_search_text, v_search_text, v_search_text, v_search_text
        );
    END IF;

    -- Add array filters
    IF array_length(v_customer_names, 1) > 0 THEN
        v_query := v_query || format('
            AND sd.customer_name = ANY(%L)',
            v_customer_names
        );
    END IF;

    IF array_length(v_statuses, 1) > 0 THEN
        v_query := v_query || format('
            AND sd.status = ANY(%L)',
            v_statuses
        );
    END IF;

    IF array_length(v_demo_approaches, 1) > 0 THEN
        v_query := v_query || format('
            AND sd.demo_approach = ANY(%L)',
            v_demo_approaches
        );
    END IF;

    IF array_length(v_demo_outcomes, 1) > 0 THEN
        v_query := v_query || format('
            AND sd.demo_outcome = ANY(%L)',
            v_demo_outcomes
        );
    END IF;

    -- Add date range filter
    IF v_start_date IS NOT NULL THEN
        v_query := v_query || format('
            AND sd.demo_date >= %L::timestamp',
            v_start_date
        );
    END IF;

    IF v_end_date IS NOT NULL THEN
        v_query := v_query || format('
            AND sd.demo_date <= %L::timestamp',
            v_end_date
        );
    END IF;

    -- Close CTE and select results with matching C# property names
    v_query := v_query || '
        )
        SELECT 
            total_records AS "TotalRecords",
            id AS "Id",
            user_created AS "UserCreated",
            date_created AS "DateCreated",
            user_updated AS "UserUpdated",
            date_updated AS "DateUpdated",
            user_id AS "UserId",
            demo_date_time AS "DemoDateTime",
            status::VARCHAR(100) AS "Status",
            customer_name::VARCHAR(255) AS "CustomerName",
            demo_name::VARCHAR(255) AS "DemoName",
            demo_contact::VARCHAR(255) AS "DemoContact",
            demo_approach::VARCHAR(255) AS "DemoApproach",
            demo_outcome::VARCHAR(255) AS "DemoOutcome",
            demo_feedback::VARCHAR(255) AS "DemoFeedback",
            comments::VARCHAR(255) AS "Comments",
            opportunity_id AS "OpportunityId",
            presenter_id AS "PresenterId",
            presenter_name::TEXT AS "PresenterName",
            address_id AS "AddressId",
            customer_id AS "CustomerId",
            opportunity_name::VARCHAR(255) AS "OpportunityName",
            address_details::TEXT AS "AddressDetails",
            user_created_name::TEXT AS "UserCreatedName",
            user_updated_name::TEXT AS "UserUpdatedName"
        FROM base_data
    ';

    -- Add ordering and pagination
    v_query := v_query || format('
        ORDER BY %I %s NULLS LAST
        LIMIT %s OFFSET %s',
        CASE 
            WHEN v_order_by = 'customerName' THEN 'CustomerName'
            WHEN v_order_by = 'demoName' THEN 'DemoName'
            WHEN v_order_by = 'demoDateTime' THEN 'DemoDateTime'
            WHEN v_order_by = 'presenterName' THEN 'PresenterName'
            WHEN v_order_by = 'demoApproach' THEN 'DemoApproach'
            WHEN v_order_by = 'demoOutcome' THEN 'DemoOutcome'
            WHEN v_order_by = 'status' THEN 'Status'
            ELSE 'DateCreated'
        END,
        v_order_direction,
        v_page_size,
        v_offset
    );

    -- Return query
    RETURN QUERY EXECUTE v_query;
END;
$$;
