-- Drop existing functions if they exist
DROP FUNCTION IF EXISTS public.fn_get_demos_grid;
DROP FUNCTION IF EXISTS public.fn_get_sales_demos_grid(jsonb);
DROP FUNCTION IF EXISTS public.fn_get_sales_demos_grid(json);

CREATE OR REPLACE FUNCTION public.fn_get_sales_demos_grid(
    p_request jsonb  -- Changed from json to jsonb
)
RETURNS TABLE (
    "TotalRecords" INTEGER,
    "Id" INTEGER,
    "UserCreated" INTEGER,
    "DateCreated" TIMESTAMP WITH TIME ZONE,
    "UserUpdated" INTEGER,
    "DateUpdated" TIMESTAMP WITH TIME ZONE,
    "UserId" INTEGER,
    "DemoDateTime" TIMESTAMP WITH TIME ZONE,
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
    v_start_date TIMESTAMP WITH TIME ZONE;
    v_end_date TIMESTAMP WITH TIME ZONE;
    v_page_number INTEGER;
    v_order_by TEXT;
    v_order_direction TEXT;
    v_query TEXT;
BEGIN
    -- Extract values from JSONB
    SELECT p_request->>'SearchText' INTO v_search_text;
    
    SELECT ARRAY(
        SELECT value 
        FROM jsonb_array_elements_text(COALESCE(p_request->'CustomerNames', '[]'::jsonb))
        WHERE value != 'string'
    ) INTO v_customer_names;
    
    SELECT ARRAY(
        SELECT value 
        FROM jsonb_array_elements_text(COALESCE(p_request->'Statuses', '[]'::jsonb))
        WHERE value != 'string'
    ) INTO v_statuses;
    
    SELECT ARRAY(
        SELECT value 
        FROM jsonb_array_elements_text(COALESCE(p_request->'DemoApproaches', '[]'::jsonb))
        WHERE value != 'string'
    ) INTO v_demo_approaches;
    
    SELECT ARRAY(
        SELECT value 
        FROM jsonb_array_elements_text(COALESCE(p_request->'DemoOutcomes', '[]'::jsonb))
        WHERE value != 'string'
    ) INTO v_demo_outcomes;
    
    -- Parse dates
    SELECT (p_request->>'StartDate')::TIMESTAMP WITH TIME ZONE INTO v_start_date;
    SELECT (p_request->>'EndDate')::TIMESTAMP WITH TIME ZONE INTO v_end_date;
    
    -- Pagination and ordering
    SELECT COALESCE((p_request->>'PageNumber')::INTEGER, 1) INTO v_page_number;
    SELECT LEAST(COALESCE((p_request->>'PageSize')::INTEGER, 10), 1000) INTO v_page_size;
    SELECT COALESCE(p_request->>'OrderBy', 'dateCreated') INTO v_order_by;
    SELECT COALESCE(UPPER(p_request->>'OrderDirection'), 'DESC') INTO v_order_direction;
      
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
                sd.demo_date,
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
    IF v_search_text IS NOT NULL AND v_search_text != 'string' AND v_search_text != '' THEN
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
            AND sd.demo_date >= %L::timestamp with time zone',
            v_start_date
        );
    END IF;

    IF v_end_date IS NOT NULL THEN
        v_query := v_query || format('
            AND sd.demo_date <= %L::timestamp with time zone',
            v_end_date
        );
    END IF;

    -- Close CTE and select results with proper C# property names
    v_query := v_query || '
        )
        SELECT 
            total_records AS "TotalRecords",
            id AS "Id",
            user_created AS "UserCreated",
            date_created AT TIME ZONE ''UTC'' AS "DateCreated",
            user_updated AS "UserUpdated",
            date_updated AT TIME ZONE ''UTC'' AS "DateUpdated",
            user_id AS "UserId",
            demo_date AT TIME ZONE ''UTC'' AS "DemoDateTime",
            status AS "Status",
            customer_name AS "CustomerName",
            demo_name AS "DemoName",
            demo_contact AS "DemoContact",
            demo_approach AS "DemoApproach",
            demo_outcome AS "DemoOutcome",
            demo_feedback AS "DemoFeedback",
            comments AS "Comments",
            opportunity_id AS "OpportunityId",
            presenter_id AS "PresenterId",
            presenter_name AS "PresenterName",
            address_id AS "AddressId",
            customer_id AS "CustomerId",
            opportunity_name AS "OpportunityName",
            address_details AS "AddressDetails",
            user_created_name AS "UserCreatedName",
            user_updated_name AS "UserUpdatedName"
        FROM base_data
    ';

    -- Add ordering and pagination with proper C# property mapping
    v_query := v_query || format('
        ORDER BY CASE %L
            WHEN ''customerName'' THEN "CustomerName"
            WHEN ''demoName'' THEN "DemoName"
            WHEN ''demoDateTime'' THEN "DemoDateTime"
            WHEN ''presenterName'' THEN "PresenterName"
            WHEN ''demoApproach'' THEN "DemoApproach"
            WHEN ''demoOutcome'' THEN "DemoOutcome"
            WHEN ''status'' THEN "Status"
            WHEN ''dateCreated'' THEN "DateCreated"
            ELSE "DateCreated"
        END %s NULLS LAST
        LIMIT %s OFFSET %s',
        v_order_by,
        v_order_direction,
        v_page_size,
        v_offset
    );

    -- Return query
    RETURN QUERY EXECUTE v_query;
END;
$$;
