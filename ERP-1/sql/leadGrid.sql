CREATE OR REPLACE FUNCTION fn_get_sales_leads(
    p_search_text VARCHAR DEFAULT NULL,
    p_zones VARCHAR[] DEFAULT NULL,
    p_customer_names VARCHAR[] DEFAULT NULL,
    p_territories VARCHAR[] DEFAULT NULL,
    p_statuses VARCHAR[] DEFAULT NULL,
    p_scores VARCHAR[] DEFAULT NULL,
    p_lead_types VARCHAR[] DEFAULT NULL,
    p_page_number INTEGER DEFAULT 1,
    p_page_size INTEGER DEFAULT 10,
    p_order_by VARCHAR DEFAULT 'created_date',
    p_order_direction VARCHAR DEFAULT 'DESC'
)
RETURNS TABLE (
    "total_records" INTEGER,
    "Id" INTEGER,
    "CustomerName" VARCHAR,
    "LeadSource" VARCHAR,
    "ReferralSourceName" VARCHAR,
    "HospitalOfReferral" VARCHAR,
    "DepartmentOfReferral" VARCHAR,
    "CityOfReferral" VARCHAR,
    "SocialMedia" VARCHAR,
    "EventDate" TIMESTAMP,
    "QualificationStatus" VARCHAR,
    "EventName" VARCHAR,
    "LeadId" VARCHAR,
    "Status" VARCHAR,
    "Score" VARCHAR,
    "InActive" BOOLEAN,
    "Comments" TEXT,
    "LeadType" VARCHAR,
    "ContactName" VARCHAR,
    "Salutation" VARCHAR,
    "ContactMobileNo" INTEGER,
    "LandLineNo" VARCHAR,
    "Email" VARCHAR,
    "Fax" VARCHAR,
    "DoorNo" VARCHAR,
    "Street" VARCHAR,
    "Landmark" VARCHAR,
    "Area" VARCHAR,
    "City" VARCHAR,
    "Website" VARCHAR,
    "Pincode" INTEGER,
    "UserCreated" UUID,
    "DateCreated" TIMESTAMP,
    "UserUpdated" UUID,
    "DateUpdated" TIMESTAMP
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_offset INTEGER;
    v_where_clause TEXT := ' WHERE 1=1 ';
    v_total_records INTEGER;
BEGIN
    -- Calculate offset
    v_offset := (p_page_number - 1) * p_page_size;

    -- Build dynamic WHERE clause
    IF p_search_text IS NOT NULL AND p_search_text != '' THEN
        v_where_clause := v_where_clause || 
        ' AND (
            LOWER(sl."customerName") LIKE ''%'' || LOWER($1) || ''%'' OR
            LOWER(sl."leadSource") LIKE ''%'' || LOWER($1) || ''%'' OR
            LOWER(sl."referralSourceName") LIKE ''%'' || LOWER($1) || ''%'' OR
            LOWER(sl."hospitalOfReferral") LIKE ''%'' || LOWER($1) || ''%'' OR
            LOWER(sl."departmentOfReferral") LIKE ''%'' || LOWER($1) || ''%'' OR
            LOWER(sl."cityOfReferral") LIKE ''%'' || LOWER($1) || ''%'' OR
            LOWER(sl."socialMedia") LIKE ''%'' || LOWER($1) || ''%'' OR
            LOWER(sl."eventName") LIKE ''%'' || LOWER($1) || ''%'' OR
            LOWER(sl."qualificationStatus") LIKE ''%'' || LOWER($1) || ''%'' OR
            LOWER(sl."leadId") LIKE ''%'' || LOWER($1) || ''%'' OR
            LOWER(sl.status) LIKE ''%'' || LOWER($1) || ''%'' OR
            LOWER(sl.score) LIKE ''%'' || LOWER($1) || ''%'' OR
            LOWER(sl."comments") LIKE ''%'' || LOWER($1) || ''%'' OR
            LOWER(sl."leadType") LIKE ''%'' || LOWER($1) || ''%'' OR
            LOWER(sl."contactName") LIKE ''%'' || LOWER($1) || ''%'' OR
            LOWER(sl.email) LIKE ''%'' || LOWER($1) || ''%'' OR
            LOWER(sl.street) LIKE ''%'' || LOWER($1) || ''%'' OR
            LOWER(sl.landmark) LIKE ''%'' || LOWER($1) || ''%'' OR
            LOWER(sl.area) LIKE ''%'' || LOWER($1) || ''%'' OR
            LOWER(sl.city) LIKE ''%'' || LOWER($1) || ''%''
        )';
    END IF;

    -- Add filter conditions
    IF p_zones IS NOT NULL AND array_length(p_zones, 1) > 0 THEN
        v_where_clause := v_where_clause || ' AND sl.area = ANY($2)';
    END IF;

    IF p_customer_names IS NOT NULL AND array_length(p_customer_names, 1) > 0 THEN
        v_where_clause := v_where_clause || ' AND sl."customerName" = ANY($3)';
    END IF;

    IF p_territories IS NOT NULL AND array_length(p_territories, 1) > 0 THEN
        v_where_clause := v_where_clause || ' AND sl.area = ANY($4)';
    END IF;

    IF p_statuses IS NOT NULL AND array_length(p_statuses, 1) > 0 THEN
        v_where_clause := v_where_clause || ' AND sl.status = ANY($5)';
    END IF;

    IF p_scores IS NOT NULL AND array_length(p_scores, 1) > 0 THEN
        v_where_clause := v_where_clause || ' AND sl.score = ANY($6)';
    END IF;

    IF p_lead_types IS NOT NULL AND array_length(p_lead_types, 1) > 0 THEN
        v_where_clause := v_where_clause || ' AND sl."leadType" = ANY($7)';
    END IF;

    -- Get total record count
    EXECUTE 'SELECT COUNT(*) FROM sales_leads sl' || v_where_clause
    INTO v_total_records
    USING p_search_text, p_zones, p_customer_names, p_territories, p_statuses, p_scores, p_lead_types;

    RETURN QUERY EXECUTE 
    'WITH base_query AS (
        SELECT 
            sl.id AS "Id",
            sl."customerName" AS "CustomerName",
            sl."leadSource" AS "LeadSource",
            sl."referralSourceName" AS "ReferralSourceName",
            sl."hospitalOfReferral" AS "HospitalOfReferral",
            sl."departmentOfReferral" AS "DepartmentOfReferral",
            sl."cityOfReferral" AS "CityOfReferral",
            sl."socialMedia" AS "SocialMedia",
            sl."eventDate" AS "EventDate",
            sl."qualificationStatus" AS "QualificationStatus",
            sl."eventName" AS "EventName",
            sl."leadId" AS "LeadId",
            sl.status AS "Status",
            sl.score AS "Score",
            sl."inActive" AS "InActive",
            sl.comments AS "Comments",
            sl."leadType" AS "LeadType",
            sl."contactName" AS "ContactName",
            sl.salutation AS "Salutation",
            sl."contactMobileNo" AS "ContactMobileNo",
            sl."landLineNo" AS "LandLineNo",
            sl.email AS "Email",
            sl.fax AS "Fax",
            sl."doorNo" AS "DoorNo",
            sl.street AS "Street",
            sl.landmark AS "Landmark",
            sl.area AS "Area",
            sl.city AS "City",
            sl.website AS "Website",
            sl.pincode AS "Pincode",
            sl.user_created AS "UserCreated",
            sl.created_date AS "DateCreated",
            sl.user_updated AS "UserUpdated",
            sl.date_updated AS "DateUpdated"
        FROM sales_leads sl' || v_where_clause || 
        ' ORDER BY sl.' || quote_ident(p_order_by) || ' ' || p_order_direction || 
        ' LIMIT ' || p_page_size || ' OFFSET ' || v_offset || '
    )
    SELECT 
        ' || v_total_records || ' AS "total_records",
        bq."Id",
        bq."CustomerName",
        bq."LeadSource",
        bq."ReferralSourceName",
        bq."HospitalOfReferral",
        bq."DepartmentOfReferral",
        bq."CityOfReferral",
        bq."SocialMedia",
        bq."EventDate",
        bq."QualificationStatus",
        bq."EventName",
        bq."LeadId",
        bq."Status",
        bq."Score",
        bq."InActive",
        bq."Comments",
        bq."LeadType",
        bq."ContactName",
        bq."Salutation",
        bq."ContactMobileNo",
        bq."LandLineNo",
        bq."Email",
        bq."Fax",
        bq."DoorNo",
        bq."Street",
        bq."Landmark",
        bq."Area",
        bq."City",
        bq."Website",
        bq."Pincode",
        bq."UserCreated",
        bq."DateCreated",
        bq."UserUpdated",
        bq."DateUpdated"
    FROM base_query bq'
    USING p_search_text, p_zones, p_customer_names, p_territories, p_statuses, p_scores, p_lead_types;
END;
$$;

-- Example usage:
SELECT * FROM fn_get_sales_leads(
    p_search_text => 'search term',
    p_zones => ARRAY['Zone1', 'Zone2'],
    p_customer_names => ARRAY['Customer1', 'Customer2'],
    p_territories => ARRAY['Territory1'],
    p_statuses => ARRAY['Active', 'Pending'],
    p_scores => ARRAY['High', 'Medium'],
    p_lead_types => ARRAY['New', 'Existing'],
    p_page_number => 1,
    p_page_size => 10,
    p_order_by => 'created_date',
    p_order_direction => 'DESC'
);