DO $$
DECLARE
    test_request jsonb := '{
        "searchText": null,
        "customerNames": [],
        "statuses": [],
        "demoApproaches": [],
        "demoOutcomes": [],
        "pageNumber": 1,
        "pageSize": 10,
        "orderBy": "date_created",
        "orderDirection": "DESC"
    }'::jsonb;
    result RECORD;
BEGIN
    -- First check if we have any demos at all
    RAISE NOTICE 'Total demos in sales_demos: %', (SELECT COUNT(*) FROM sales_demos);
    
    -- Test the function
    FOR result IN SELECT * FROM fn_get_sales_demos_grid(test_request) LOOP
        RAISE NOTICE 'Found demo: %, %', result.id, result.demo_name;
    END LOOP;
END $$;
