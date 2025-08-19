CREATE TABLE public.sales_opportunities (
    id SERIAL PRIMARY KEY,
    user_created INT,
    date_created TIMESTAMP,
    user_updated INT,
    date_updated TIMESTAMP,
    status VARCHAR(255),
    expected_completion DATE,
    opportunity_type VARCHAR(255),
    opportunity_for VARCHAR(255),
    customer_id VARCHAR(255),
    customer_name VARCHAR(255),
    customer_type VARCHAR(255),
    opportunity_name VARCHAR(255),
    opportunity_id VARCHAR(255) UNIQUE NOT NULL,
    comments TEXT,
    isactive BOOLEAN DEFAULT FALSE NOT NULL,
    lead_id VARCHAR(255),
    sales_representative_id INT,
    contact_name VARCHAR(255),
    contact_mobile_no VARCHAR(255)
);
ALTER TABLE public.sales_opportunities ADD CONSTRAINT fk_sales_opp_user_created FOREIGN KEY (user_created) REFERENCES public.users(user_id);
ALTER TABLE public.sales_opportunities ADD CONSTRAINT fk_sales_opp_user_updated FOREIGN KEY (user_updated) REFERENCES public.users(user_id);


