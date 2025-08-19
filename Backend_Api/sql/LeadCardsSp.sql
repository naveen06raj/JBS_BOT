-- Create stored procedure for Lead Cards count by status
DROP FUNCTION IF EXISTS sp_get_lead_cards_count();

CREATE OR REPLACE FUNCTION sp_get_lead_cards_count()
RETURNS TABLE (
    status VARCHAR,
    count BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        sl.status,
        COUNT(*)::BIGINT AS count
    FROM 
        public.sales_lead sl
    WHERE 
        sl.isactive = true
    GROUP BY 
        sl.status
    HAVING 
        sl.status IN ('Prospecting', 'Qualified', 'Negotiation', 'Closed Won');
END;
$$ LANGUAGE plpgsql;
