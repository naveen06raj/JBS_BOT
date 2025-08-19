-- Check if we have any demo data
DO $$
DECLARE
    demo_count INTEGER;
    user_id INTEGER;
BEGIN
    SELECT COUNT(*) INTO demo_count FROM public.sales_demos;
    RAISE NOTICE 'Current demo count: %', demo_count;
    
    IF demo_count = 0 THEN
        -- Get a valid user_id for foreign key constraint
        SELECT user_id INTO user_id FROM users LIMIT 1;
        
        -- Insert some test data if no demos exist
        INSERT INTO public.sales_demos 
        (user_created, user_updated, user_id, demo_date, status, customer_name, demo_name, demo_approach, demo_outcome, demo_feedback, comments, presenter_id)
        VALUES
        (user_id, user_id, user_id, NOW(), 'Pending', 'Test Customer 1', 'Demo 1', 'Online', 'Success', 'Good feedback', 'Test comments', user_id),
        (user_id, user_id, user_id, NOW(), 'Completed', 'Test Customer 2', 'Demo 2', 'In-Person', 'Success', 'Excellent feedback', 'Test comments', user_id),
        (user_id, user_id, user_id, NOW(), 'Scheduled', 'Test Customer 3', 'Demo 3', 'Online', 'Pending', NULL, NULL, user_id);
        
        RAISE NOTICE 'Inserted test demo data';
    END IF;
END $$;
