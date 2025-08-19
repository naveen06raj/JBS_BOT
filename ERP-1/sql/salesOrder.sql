CREATE TABLE public.sales_orders (
    id SERIAL PRIMARY KEY,                                    -- Internal unique identifier
    order_id VARCHAR(20) UNIQUE NOT NULL,                     -- e.g., "SO-2025-0001"
    customer_id INT REFERENCES sales_customers(id),                 -- FK to customers table    order_date TIMESTAMP WITH TIME ZONE NOT NULL,
    expected_delivery_date TIMESTAMP WITH TIME ZONE,
    status VARCHAR(50) NOT NULL,                              -- e.g., "Draft", "Confirmed"
    quotation_id INT REFERENCES sales_quotations(id),               -- Optional FK to quotations
    po_id VARCHAR(50),                                        -- Customer's PO reference
    acceptance_date TIMESTAMP WITH TIME ZONE,
    total_amount NUMERIC(12, 2) DEFAULT 0.00,
    tax_amount NUMERIC(12, 2) DEFAULT 0.00,
    grand_total NUMERIC(12, 2) DEFAULT 0.00,
    notes TEXT,
   user_created int4 NULL,    date_created TIMESTAMP WITH TIME ZONE NULL,
    user_updated int4 NULL,
    date_updated TIMESTAMP WITH TIME ZONE NULL

);
alter table sales_orders 
 ADD CONSTRAINT fk_so_user_created FOREIGN KEY (user_created) REFERENCES public.sales_employees(id),
    ADD CONSTRAINT fk_soo_user_updated FOREIGN KEY (user_updated) REFERENCES public.sales_employees(id);

-- Function to get next order number sequence
-- Create sequence for order numbers
CREATE SEQUENCE IF NOT EXISTS sales_order_seq;

-- Function to generate unique order ID
CREATE OR REPLACE FUNCTION fn_generate_order_id()
RETURNS VARCHAR AS $$
DECLARE
    v_year VARCHAR;
    v_sequence INTEGER;
    v_order_id VARCHAR;
BEGIN
    -- Get current year
    v_year := to_char(CURRENT_DATE, 'YYYY');
    
    -- Get next sequence number
    v_sequence := nextval('sales_order_seq');
    
    -- Format the order ID
    v_order_id := 'SO-' || v_year || '-' || LPAD(v_sequence::text, 4, '0');
    
    RETURN v_order_id;
END;
$$ LANGUAGE plpgsql;

-- Function to create a new sales order
CREATE OR REPLACE FUNCTION fn_create_sales_order(
    p_customer_id INT,
    p_order_date TIMESTAMP WITH TIME ZONE,
    p_expected_delivery_date TIMESTAMP WITH TIME ZONE,
    p_status VARCHAR,
    p_quotation_id INT,
    p_po_id VARCHAR,
    p_acceptance_date TIMESTAMP WITH TIME ZONE,
    p_total_amount NUMERIC,
    p_tax_amount NUMERIC,
    p_grand_total NUMERIC,
    p_notes TEXT,
    p_user_created INT
)
RETURNS SETOF sales_orders AS $$
DECLARE
    v_order_id VARCHAR;
    v_result sales_orders;
BEGIN
    -- Generate unique order ID
    v_order_id := fn_generate_order_id();
    
    INSERT INTO sales_orders (
        order_id,
        customer_id,
        order_date,
        expected_delivery_date,
        status,
        quotation_id,
        po_id,
        acceptance_date,
        total_amount,
        tax_amount,
        grand_total,
        notes,
        user_created,
        date_created
    )
    VALUES (
        v_order_id,
        p_customer_id,
        p_order_date,
        p_expected_delivery_date,
        p_status,
        p_quotation_id,
        p_po_id,
        p_acceptance_date,
        p_total_amount,
        p_tax_amount,
        p_grand_total,
        p_notes,
        p_user_created,
        CURRENT_TIMESTAMP
    )
    RETURNING * INTO v_result;
    
    RETURN NEXT v_result;
END;
$$ LANGUAGE plpgsql;
