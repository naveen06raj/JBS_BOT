-- Create a sequence for lead IDs if it doesn't exist
CREATE SEQUENCE IF NOT EXISTS lead_id_seq START WITH 1 INCREMENT BY 1;

-- Reset sequence to 1 if needed (uncomment if you want to reset)
-- ALTER SEQUENCE lead_id_seq RESTART WITH 1;

-- Function to generate the next lead ID
CREATE OR REPLACE FUNCTION get_next_lead_id()
RETURNS varchar AS $$
DECLARE
    next_val integer;
BEGIN
    -- Get next value from sequence
    SELECT nextval('lead_id_seq') INTO next_val;
    -- Return formatted lead ID
    RETURN 'LD' || LPAD(next_val::text, 5, '0');
END;
$$ LANGUAGE plpgsql;
