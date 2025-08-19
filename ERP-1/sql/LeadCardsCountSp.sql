-- Create stored procedure for Lead Cards count
DROP FUNCTION IF EXISTS sp_get_lead_cards_count();

CREATE OR REPLACE FUNCTION sp_get_lead_cards_count()
RETURNS TABLE (
    total_leads BIGINT,
    new_this_week BIGINT,
    qualified_leads BIGINT,
    converted_leads BIGINT
) AS $$
DECLARE
    current_week_start DATE;
    current_week_end DATE;
BEGIN
    -- Get current week's start and end dates
    current_week_start := date_trunc('week', CURRENT_DATE)::DATE;
    current_week_end := (current_week_start + INTERVAL '6 days')::DATE;

    RETURN QUERY
    WITH lead_counts AS (
        SELECT 
            COUNT(*) FILTER (WHERE isactive = true) as total_count,
            COUNT(*) FILTER (WHERE 
                isactive = true 
                AND date_created::DATE BETWEEN current_week_start AND current_week_end
            ) as new_this_week_count,
            COUNT(*) FILTER (WHERE 
                isactive = true 
                AND status = 'Qualified'
            ) as qualified_count,
            COUNT(*) FILTER (WHERE 
                isactive = true 
                AND status = 'Converted'
            ) as converted_count
        FROM 
            public.sales_lead
    )
    SELECT 
        total_count as total_leads,
        new_this_week_count as new_this_week,
        qualified_count as qualified_leads,
        converted_count as converted_leads
    FROM lead_counts;
END;
$$ LANGUAGE plpgsql;
