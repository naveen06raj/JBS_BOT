-- 1. Drop existing foreign key constraints
ALTER TABLE sales_quotations DROP CONSTRAINT IF EXISTS fk_sales_quotation_user_created;
ALTER TABLE sales_quotations DROP CONSTRAINT IF EXISTS fk_sales_quotation_user_updated;

-- 2. Add the constraints back with ON DELETE SET NULL
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

-- 3. Create a hard delete function for quotations
CREATE OR REPLACE FUNCTION hard_delete_quotation(
    p_id INT
)
RETURNS BOOLEAN AS $$
BEGIN
    -- First, handle any related records in quotation_products if they exist
    DELETE FROM quotation_products WHERE quotation_id = p_id;
    
    -- Then delete the quotation itself
    DELETE FROM sales_quotations WHERE id = p_id;
    
    RETURN FOUND;
END;
$$ LANGUAGE plpgsql;

-- 4. Update the existing soft delete function to handle NULL users
CREATE OR REPLACE FUNCTION delete_quotation(
    p_id INT,
    p_user_updated INT
)
RETURNS BOOLEAN AS $$
BEGIN
    UPDATE public.sales_quotations
    SET 
        is_active = false,
        user_updated = COALESCE(p_user_updated, user_updated), -- Keep existing user if new one is NULL
        date_updated = CURRENT_TIMESTAMP
    WHERE id = p_id AND is_active = true;

    RETURN FOUND;
END;
$$ LANGUAGE plpgsql;
