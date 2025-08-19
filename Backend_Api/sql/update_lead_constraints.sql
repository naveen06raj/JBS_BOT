-- Drop existing constraints
ALTER TABLE public.sales_contacts DROP CONSTRAINT IF EXISTS fk_sales_contacts_sales_lead;
ALTER TABLE public.sales_leads_business_challenges DROP CONSTRAINT IF EXISTS fk_sales_leads;
ALTER TABLE public.sales_quotations DROP CONSTRAINT IF EXISTS fk_sales_quotation_lead;
ALTER TABLE public.sales_orders DROP CONSTRAINT IF EXISTS sales_orders_quotation_id_fkey;
ALTER TABLE public.sales_addresses DROP CONSTRAINT IF EXISTS fk_sales_addresses_sales_lead;

-- Add constraints with CASCADE delete
ALTER TABLE public.sales_contacts 
    ADD CONSTRAINT fk_sales_contacts_sales_lead 
    FOREIGN KEY (sales_lead_id) 
    REFERENCES public.sales_lead(id) 
    ON DELETE CASCADE;

ALTER TABLE public.sales_leads_business_challenges 
    ADD CONSTRAINT fk_sales_leads 
    FOREIGN KEY (sales_leads_id) 
    REFERENCES public.sales_lead(id) 
    ON DELETE CASCADE;

ALTER TABLE public.sales_quotations 
    ADD CONSTRAINT fk_sales_quotation_lead 
    FOREIGN KEY (lead_id) 
    REFERENCES public.sales_lead(id) 
    ON DELETE CASCADE;

-- Add cascade delete for sales_orders when quotation is deleted
ALTER TABLE public.sales_orders
    ADD CONSTRAINT sales_orders_quotation_id_fkey
    FOREIGN KEY (quotation_id)
    REFERENCES public.sales_quotations(id)
    ON DELETE CASCADE;

-- Note: quotation_products already has ON DELETE CASCADE in its definition
-- Note: sales_lead_interestedproducts already has CASCADE delete
-- Note: sales_addresses appears to not have a foreign key constraint defined yet, so let's add it
ALTER TABLE public.sales_addresses 
    ADD CONSTRAINT fk_sales_addresses_sales_lead 
    FOREIGN KEY (sales_lead_id) 
    REFERENCES public.sales_lead(id) 
    ON DELETE CASCADE;
