CREATE TABLE public.sales_demos (
    id SERIAL PRIMARY KEY,
    user_created INT,
    date_created TIMESTAMP DEFAULT NOW(),
    user_updated INT,
    date_updated TIMESTAMP,
    -- From demo_requests
    user_id INT,                      -- Agent/Manager who created request
    demo_date TIMESTAMP,
    status VARCHAR(100),             -- Combined status field
    address_id INT,
    opportunity_id INT,
    customer_id INT,
    -- From sales_demos
    demo_contact VARCHAR(255),
    customer_name VARCHAR(255),
    demo_name VARCHAR(255),
    demo_approach VARCHAR(255),
    demo_outcome VARCHAR(255),
    demo_feedback varchar(255),
    comments Varchar(255),
    presenter_id INT,

    -- Foreign Key Constraints
    CONSTRAINT fk_demo_user_created FOREIGN KEY (user_created) REFERENCES users(user_id),
    CONSTRAINT fk_demo_user_updated FOREIGN KEY (user_updated) REFERENCES users(user_id),
    CONSTRAINT fk_demo_user_id FOREIGN KEY (user_id) REFERENCES users(user_id),
    CONSTRAINT fk_demo_presenter FOREIGN KEY (presenter_id) REFERENCES users(user_id),
    CONSTRAINT fk_demo_address FOREIGN KEY (address_id) REFERENCES sales_addresses(id),
    CONSTRAINT fk_demo_opportunity FOREIGN KEY (opportunity_id) REFERENCES sales_opportunities(id),
    CONSTRAINT fk_demo_customer FOREIGN KEY (customer_id) REFERENCES sales_customers(id)
);
-- Indexes for performance
