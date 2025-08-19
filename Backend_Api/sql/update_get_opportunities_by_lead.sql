CREATE OR REPLACE FUNCTION fn_getopportunitiesbyleadid(p_lead_id varchar)
RETURNS TABLE (
    id integer,
    opportunity_id varchar,
    opportunity_name varchar,
    customer_name varchar,
    status varchar,
    expected_completion date,
    opportunity_type varchar,
    date_created timestamp,
    comments text,
    isactive boolean
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        o.id,
        o.opportunity_id,
        o.opportunity_name,
        o.customer_name,
        o.status,
        o.expected_completion,
        o.opportunity_type,
        o.date_created,
        o.comments,
        o.isactive
    FROM sales_opportunities o
    WHERE o.lead_id = p_lead_id
    AND o.isactive = true
    ORDER BY o.date_created DESC;
END;
$$ LANGUAGE plpgsql;
