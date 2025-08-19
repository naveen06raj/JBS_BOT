-- Drop existing function if it exists
DROP FUNCTION IF EXISTS sp_delete_lead_cascade(INTEGER);

-- Create function to handle deletion of lead and related quotations
CREATE OR REPLACE FUNCTION sp_delete_lead_cascade(p_lead_id INTEGER)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    -- First, break parent-child relationships for quotations related to this lead
    WITH RECURSIVE quotation_tree AS (
        -- Get all quotations for this lead
        SELECT id, parent_sales_quotations_id
        FROM sales_quotations
        WHERE lead_id = p_lead_id
        UNION ALL
        -- Get child quotations
        SELECT sq.id, sq.parent_sales_quotations_id
        FROM sales_quotations sq
        INNER JOIN quotation_tree qt ON sq.parent_sales_quotations_id = qt.id
    )
    UPDATE sales_quotations
    SET parent_sales_quotations_id = NULL
    WHERE id IN (SELECT id FROM quotation_tree);

    -- Now delete all quotations associated with the lead
    DELETE FROM sales_quotations
    WHERE lead_id = p_lead_id;

    -- Finally delete the lead
    DELETE FROM sales_lead
    WHERE id = p_lead_id;
END;
$$;
