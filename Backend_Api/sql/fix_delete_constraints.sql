-- First, we need to drop the existing foreign key constraints
ALTER TABLE sales_quotations DROP CONSTRAINT IF EXISTS fk_sales_quotation_user_created;
ALTER TABLE sales_quotations DROP CONSTRAINT IF EXISTS fk_sales_quotation_user_updated;
ALTER TABLE sales_quotations DROP CONSTRAINT IF EXISTS fk_sales_quotation_opportunity;
ALTER TABLE sales_quotations DROP CONSTRAINT IF EXISTS fk_sales_quotation_customer;
ALTER TABLE sales_quotations DROP CONSTRAINT IF EXISTS fk_sales_quotation_parent;
ALTER TABLE sales_quotations DROP CONSTRAINT IF EXISTS fk_sales_quotation_lead;

-- Re-create the constraints with proper ON DELETE behavior
ALTER TABLE sales_quotations 
ADD CONSTRAINT fk_sales_quotation_user_created 
FOREIGN KEY (user_created) 
REFERENCES users(user_id)
ON DELETE SET NULL;

ALTER TABLE sales_quotations 
ADD CONSTRAINT fk_sales_quotation_user_updated 
FOREIGN KEY (user_updated) 
REFERENCES users(user_id)
ON DELETE SET NULL;

ALTER TABLE sales_quotations 
ADD CONSTRAINT fk_sales_quotation_opportunity 
FOREIGN KEY (opportunity_id) 
REFERENCES sales_opportunities(id)
ON DELETE CASCADE;

ALTER TABLE sales_quotations 
ADD CONSTRAINT fk_sales_quotation_customer 
FOREIGN KEY (customer_id) 
REFERENCES sales_customers(id)
ON DELETE SET NULL;

ALTER TABLE sales_quotations 
ADD CONSTRAINT fk_sales_quotation_parent 
FOREIGN KEY (parent_sales_quotations_id) 
REFERENCES sales_quotations(id)
ON DELETE SET NULL;

ALTER TABLE sales_quotations 
ADD CONSTRAINT fk_sales_quotation_lead 
FOREIGN KEY (lead_id) 
REFERENCES sales_lead(id)
ON DELETE SET NULL;

-- Update the delete stored procedure to handle this better
CREATE OR REPLACE FUNCTION delete_quotation(
    p_id INT,
    p_user_updated INT DEFAULT NULL
)
RETURNS BOOLEAN AS $$
DECLARE
    has_products BOOLEAN;
BEGIN
    -- First check if there are any quotation products
    SELECT EXISTS (
        SELECT 1 
        FROM quotation_products 
        WHERE quotation_id = p_id
    ) INTO has_products;

    -- If there are products, delete them first
    IF has_products THEN
        DELETE FROM quotation_products WHERE quotation_id = p_id;
    END IF;

    -- Now update the quotation to mark it as inactive
    UPDATE public.sales_quotations
    SET 
        is_active = false,
        user_updated = p_user_updated,
        date_updated = CURRENT_TIMESTAMP
    WHERE id = p_id AND is_active = true;

    RETURN FOUND;
END;
$$ LANGUAGE plpgsql;
