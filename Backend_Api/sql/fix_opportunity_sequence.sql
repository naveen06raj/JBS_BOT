-- First remove the default value from the table
ALTER TABLE sales_opportunities 
    ALTER COLUMN opportunity_id DROP DEFAULT;

-- Now we can safely drop and recreate the sequence and function
DROP SEQUENCE IF EXISTS opportunity_id_seq CASCADE;
DROP FUNCTION IF EXISTS generate_opportunity_id();

-- Create new sequence
CREATE SEQUENCE opportunity_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- Set proper ownership and permissions
ALTER SEQUENCE opportunity_id_seq OWNER TO postgres;
GRANT ALL ON SEQUENCE opportunity_id_seq TO postgres;
GRANT USAGE, SELECT ON SEQUENCE opportunity_id_seq TO PUBLIC;

-- Create function to generate opportunity IDs
CREATE OR REPLACE FUNCTION generate_opportunity_id() 
RETURNS varchar AS $$
BEGIN
    -- Add proper error handling
    BEGIN
        RETURN 'OPP' || LPAD(nextval('opportunity_id_seq')::text, 5, '0');
    EXCEPTION 
        WHEN OTHERS THEN
            -- Log error if needed
            RAISE NOTICE 'Error in generate_opportunity_id: %', SQLERRM;
            -- Re-raise the error
            RAISE;
    END;
END;
$$ LANGUAGE plpgsql
SECURITY DEFINER; -- Run with definer's privileges

-- Grant execute permission on function
GRANT EXECUTE ON FUNCTION generate_opportunity_id() TO PUBLIC;
