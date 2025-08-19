-- Drop the existing foreign key constraint
ALTER TABLE public.sales_contacts DROP CONSTRAINT IF EXISTS fk_sales_contacts_sales_lead;

-- Add the new constraint with CASCADE delete
ALTER TABLE public.sales_contacts ADD CONSTRAINT fk_sales_contacts_sales_lead 
    FOREIGN KEY (sales_lead_id) 
    REFERENCES public.sales_lead(id) 
    ON DELETE CASCADE;
