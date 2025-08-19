-- Fill in any missing opportunity IDs
WITH numbered_rows AS (
    SELECT id, 'OPP' || LPAD(ROW_NUMBER() OVER (ORDER BY id)::text, 5, '0') as new_opp_id
    FROM sales_opportunities so
    WHERE opportunity_id IS NULL OR opportunity_id = ''
)
UPDATE sales_opportunities
SET opportunity_id = numbered_rows.new_opp_id
FROM numbered_rows
WHERE sales_opportunities.id = numbered_rows.id;

-- Reset the sequence to the next value after the highest used
SELECT setval('opportunity_id_seq', 
    COALESCE((
        SELECT MAX(NULLIF(regexp_replace(opportunity_id, '[^0-9]', '', 'g'), '')::int)
        FROM sales_opportunities
    ), 0) + 1
);
