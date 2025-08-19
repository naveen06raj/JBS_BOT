-- Add unit_price column to sales_products table
ALTER TABLE sales_products
ADD COLUMN unit_price numeric(12,2) NULL;

-- Update existing records to set unit_price based on amount and quantity if needed
UPDATE sales_products
SET unit_price = CASE 
    WHEN qty > 0 THEN CAST(amount AS numeric(12,2)) / qty
    ELSE NULL
END
WHERE amount IS NOT NULL AND qty IS NOT NULL AND qty > 0;
