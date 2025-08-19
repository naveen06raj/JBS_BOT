CREATE TABLE item_master (
    id SERIAL PRIMARY KEY,   
    user_created INT4 NULL,
    date_created TIMESTAMP NULL,
    user_updated INT4 NULL,
    date_updated TIMESTAMP NULL,
    sku VARCHAR(100) NOT NULL UNIQUE,
    description VARCHAR(255),
    category VARCHAR(255),
    brand VARCHAR(255),
    make VARCHAR(255),
    model VARCHAR(255),
    item_name VARCHAR(255),
    item_code VARCHAR(255),
    default_purchase_price DECIMAL(10, 2),
    default_sale_price DECIMAL(10, 2),
    unit_of_measure VARCHAR(50) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    reorder_level INT,
    maximum_stock_level INT,
    image_url VARCHAR(255)

);

ALTER TABLE item_master
    ADD CONSTRAINT fk_item_master_user_created FOREIGN KEY (user_created) REFERENCES users(user_id),
    ADD CONSTRAINT fk_item_master_user_updated FOREIGN KEY (user_updated) REFERENCES users(user_id);

alter table item_master 
add column product varchar(255);

-- Step 1: Create the table with snake_case and corrected syntax
CREATE TABLE public.sales_product (
    id SERIAL PRIMARY KEY,
    user_created INT4 NULL,
    date_created TIMESTAMP NULL,
    user_updated INT4 NULL,
    date_updated TIMESTAMP NULL,

    qty INT4 NULL,
    amount FLOAT8 NULL,
    is_active BOOL DEFAULT TRUE, -- Changed from 'isactive' to 'is_active'

    item_id INT4 REFERENCES item_master(id), -- Corrected 'refernces to' to 'REFERENCES'
    stage VARCHAR(255),
    unit_price NUMERIC(12, 2) NULL,
    stage_item_id INT4 NULL
);

-- Step 2: Add foreign key constraints
ALTER TABLE public.sales_product
    ADD CONSTRAINT fk_sales_product_user_created
    FOREIGN KEY (user_created) REFERENCES users(user_id);

ALTER TABLE public.sales_product
    ADD CONSTRAINT fk_sales_product_user_updated
    FOREIGN KEY (user_updated) REFERENCES users(user_id);
