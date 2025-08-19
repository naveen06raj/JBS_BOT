-- Update opportunity_id for any records that don't have one
WITH numbered_rows AS (
    SELECT id, 'OPP' || LPAD(ROW_NUMBER() OVER (ORDER BY id)::text, 5, '0') as new_opp_id
    FROM public.sales_opportunities
    WHERE opportunity_id IS NULL
)
UPDATE public.sales_opportunities
SET opportunity_id = numbered_rows.new_opp_id
FROM numbered_rows
WHERE sales_opportunities.id = numbered_rows.id;

-- Make opportunity_id not nullable
ALTER TABLE public.sales_opportunities ALTER COLUMN opportunity_id SET NOT NULL;
