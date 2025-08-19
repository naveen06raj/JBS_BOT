
-- Table: make
CREATE TABLE IF NOT EXISTS public.makes (
    id SERIAL PRIMARY KEY,
    user_created UUID,
    date_created TIMESTAMP,
    user_updated UUID,
    date_updated TIMESTAMP,
    name VARCHAR(255)
);

-- Table: model
CREATE TABLE IF NOT EXISTS public.models (
    id SERIAL PRIMARY KEY,
    user_created UUID,
    date_created TIMESTAMP,
    user_updated UUID,
    date_updated TIMESTAMP,
    name VARCHAR(255)
);

-- Table: category
CREATE TABLE IF NOT EXISTS public.categories (
    id SERIAL PRIMARY KEY,
    user_created INTEGER,
    date_created TIMESTAMP,
    user_updated INTEGER,
    date_updated TIMESTAMP,
    name VARCHAR(255)
);

-- Table: products
CREATE TABLE IF NOT EXISTS public.products (
    id SERIAL PRIMARY KEY,
    user_created INTEGER,
    date_created TIMESTAMP,
    user_updated INTEGER,
    date_updated TIMESTAMP,
    name VARCHAR(255)
);

CREATE TABLE sales_countries (
  id SERIAL PRIMARY KEY,
  user_created INTEGER REFERENCES sales_employees(id),
  date_created TIMESTAMP,
  user_updated INTEGER REFERENCES sales_employees(id),
  date_updated TIMESTAMP,
  name VARCHAR(255),
  description TEXT 
);
CREATE TABLE sales_states (
  id SERIAL PRIMARY KEY,
  user_created INTEGER REFERENCES sales_employees(id),
  date_created TIMESTAMP,
  user_updated INTEGER REFERENCES sales_employees(id),
  date_updated TIMESTAMP,
  name VARCHAR(255),
  sales_countries_id INTEGER REFERENCES sales_countries(id)
);
CREATE TABLE sales_territories (
  id SERIAL PRIMARY KEY,
  user_created INTEGER REFERENCES sales_employees(id),
  date_created TIMESTAMP,
  user_updated INTEGER REFERENCES sales_employees(id),
  date_updated TIMESTAMP,
  name VARCHAR(255),
  alias VARCHAR(255),

)

CREATE TABLE sales_districts (
  id SERIAL PRIMARY KEY NOT NULL,
  user_created INTEGER REFERENCES sales_employees(id),
  date_created TIMESTAMP,
  user_updated INTEGER REFERENCES sales_employees(id),
  date_updated TIMESTAMP,
  name VARCHAR(255),
  sales_territories_id INTEGER REFERENCES sales_territories(id),
    sales_states_id INTEGER REFERENCES sales_states(id)
);

CREATE TABLE sales_cities (
  id SERIAL PRIMARY KEY,
  user_created INTEGER REFERENCES sales_employees(id),
  date_created TIMESTAMP,
  user_updated INTEGER REFERENCES sales_employees(id),
  date_updated TIMESTAMP,
  name VARCHAR(255),
  sales_districts_id INTEGER REFERENCES sales_districts(id),
city_code varchar(255) null,
description text null
);




CREATE TABLE sales_areas (
  id SERIAL PRIMARY KEY,
  user_created INTEGER REFERENCES sales_employees(id),
  date_created TIMESTAMP,
  user_updated INTEGER REFERENCES sales_employees(id),
  date_updated TIMESTAMP,
  name VARCHAR(255),
  sales_cities_id INTEGER REFERENCES sales_cities(id),
pincode int null,
description text null
);


CREATE TABLE pincodes (
  id SERIAL PRIMARY KEY,
  user_created INTEGER REFERENCES sales_employees(id),
  date_created TIMESTAMP,
  user_updated INTEGER REFERENCES sales_employees(id),
  date_updated TIMESTAMP,
  pincode VARCHAR(10),
  sales_areas_id INTEGER REFERENCES sales_areas(id)
);



CREATE TABLE sales_employees (
  id SERIAL PRIMARY KEY NOT NULL,
  user_created INTEGER,
  date_created TIMESTAMP,
  user_updated INTEGER,
  date_updated TIMESTAMP,
  employee_id VARCHAR(255),
  name VARCHAR(255),
  company VARCHAR(255),
  department VARCHAR(255),
  role VARCHAR(255),
  department_type VARCHAR(255),
  marital_status VARCHAR(255),
  joining_date DATE,
  gender VARCHAR(255),
  mobile_no VARCHAR(255),
  company_mobile VARCHAR(255),
  emergency_contact_name VARCHAR(255),
  emergency_phone VARCHAR(255),
  relation VARCHAR(255),
  email_id VARCHAR(255),
  company_email_id VARCHAR(255),
  company_mobile_imei_no VARCHAR(255),
  company_mobile_model VARCHAR(255),
  report_to VARCHAR(255),
  status_id VARCHAR(255),
  family VARCHAR(255),
  health_details VARCHAR(255),
  language_known VARCHAR(255),
  home_address TEXT,
  sales_territories_id INTEGER,
  isactive BOOLEAN DEFAULT FALSE
);

CREATE TABLE inventory_item_categories (
    id SERIAL PRIMARY KEY NOT NULL,
    user_created INTEGER,
    date_created TIMESTAMP,
    user_updated INTEGER,
    date_updated TIMESTAMP,
    description TEXT,
    name TEXT,
    isactive BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (user_created) REFERENCES sales_employees(id),
    FOREIGN KEY (user_updated) REFERENCES sales_employees(id)
);


CREATE TABLE IF NOT EXISTS public.inventory_items
(
    id SERIAL PRIMARY KEY,
    user_created INTEGER,
    date_created TIMESTAMP,
    user_updated INTEGER,
    date_updated TIMESTAMP,
    quantity INTEGER,
    hsn VARCHAR(255),
    rack VARCHAR(255),
    shelf VARCHAR(255),
    "column" VARCHAR(255),
    brand VARCHAR(255),
    uom INTEGER,
    status VARCHAR(255),
    item_code VARCHAR(255),
    item_name VARCHAR(255),
    item_description VARCHAR(255),
    internal_serial_num VARCHAR(255),
    external_serial_num VARCHAR(255),
    tax_percentage VARCHAR(255),
    critical VARCHAR(255),
    parent_items_code VARCHAR(255),
    valuation_method VARCHAR(255),
    category_no VARCHAR(255),
    standard_selling_rate VARCHAR(255),
    minimum_selling_rate VARCHAR(255),
    unit_of_measures VARCHAR(255),
    group_of_item BOOLEAN,
    supplier_id VARCHAR(255),
    sales_account VARCHAR(255),
    safety_stock VARCHAR(255),
    buying_unit_of_measure VARCHAR(255),
    item_full_name VARCHAR(255),
    consumption_uom VARCHAR(255),
    buom_to_uom VARCHAR(255),
    item_for VARCHAR(255),
    cuom_to_uom VARCHAR(255),
    reorder_qty VARCHAR(255),
    purchase_account VARCHAR(255),
    isactive BOOLEAN DEFAULT FALSE,
    parent_inventory_items_id INTEGER,
    make_id INTEGER,
    model_id INTEGER,
    product_id INTEGER,
    category_id INTEGER,

    -- Foreign Keys
    CONSTRAINT fk_user_created FOREIGN KEY (user_created)
        REFERENCES public.sales_employees (id),
    CONSTRAINT fk_user_updated FOREIGN KEY (user_updated)
        REFERENCES public.sales_employees (id),
    CONSTRAINT fk_parent_inventory_item FOREIGN KEY (parent_inventory_items_id)
        REFERENCES public.inventory_items (id),
    CONSTRAINT fk_make FOREIGN KEY (make_id)
        REFERENCES public.makes (id),
    CONSTRAINT fk_model FOREIGN KEY (model_id)
        REFERENCES public.models (id),
    CONSTRAINT fk_product FOREIGN KEY (product_id)
        REFERENCES public.products (id),
    CONSTRAINT fk_category FOREIGN KEY (category_id)
        REFERENCES public.categories (id)
);



CREATE TABLE sales_leads (
    id SERIAL PRIMARY KEY NOT NULL,
    user_created INTEGER,
    date_created TIMESTAMP,
    user_updated INTEGER,
    date_updated TIMESTAMP,
    customer_name VARCHAR(255),
    lead_source VARCHAR(255),
    referral_source_name VARCHAR(255),
    hospital_of_referral VARCHAR(255),
    department_of_referral VARCHAR(255),
    social_media VARCHAR(255),
    event_date DATE,
    qualification_status VARCHAR(255),
    event_name VARCHAR(255),
    lead_id VARCHAR(255),
    status VARCHAR(255),
    score VARCHAR(255),
    isactive BOOLEAN DEFAULT FALSE,
    comments TEXT,
    lead_type VARCHAR(255),
    contact_name VARCHAR(100),
    salutation VARCHAR(10),
    contact_mobile_no VARCHAR(15),
    land_line_no VARCHAR(15),
    email VARCHAR(30),
    fax VARCHAR(15),
    door_no VARCHAR(5),
    street VARCHAR(50),
    landmark VARCHAR(50),
    website VARCHAR(100),

    -- Replaced flat columns with normalized references
    territory_id INTEGER,
    area_id INTEGER,
    city_id INTEGER,
    pincode_id INTEGER,
    city_of_referral_id INTEGER,
    district_id INTEGER,
    state_id INTEGER,

    -- Foreign Keys
    CONSTRAINT fk_user_created FOREIGN KEY (user_created) REFERENCES sales_employees(id),
    CONSTRAINT fk_user_updated FOREIGN KEY (user_updated) REFERENCES sales_employees(id),
    CONSTRAINT fk_sales_lead_territory FOREIGN KEY (territory_id) REFERENCES sales_territories(id),
    CONSTRAINT fk_sales_lead_area FOREIGN KEY (area_id) REFERENCES sales_areas(id),
    CONSTRAINT fk_sales_lead_city FOREIGN KEY (city_id) REFERENCES sales_cities(id),
    CONSTRAINT fk_sales_lead_pincode FOREIGN KEY (pincode_id) REFERENCES pincodes(id),
    CONSTRAINT fk_sales_lead_city_of_referral FOREIGN KEY (city_of_referral_id) REFERENCES sales_cities(id),
    CONSTRAINT fk_sales_lead_district FOREIGN KEY (district_id) REFERENCES sales_districts(id),
    CONSTRAINT fk_sales_lead_state FOREIGN KEY (state_id) REFERENCES sales_states(id)
);




-- Table: public.sales_contacts

-- DROP TABLE IF EXISTS public.sales_contacts;

CREATE TABLE IF NOT EXISTS public.sales_contacts
(
    id integer NOT NULL DEFAULT nextval('sales_contacts_id_seq'::regclass),
    user_created integer,
    date_created timestamp without time zone,
    user_updated integer,
    date_updated timestamp without time zone,
    contact_name character varying(255) COLLATE pg_catalog."default",
    department_name character varying(255) COLLATE pg_catalog."default",
    specialist character varying(255) COLLATE pg_catalog."default",
    degree character varying(255) COLLATE pg_catalog."default",
    email character varying(255) COLLATE pg_catalog."default",
    mobile_no character varying(255) COLLATE pg_catalog."default",
    
    isactive boolean NOT NULL DEFAULT false,
    own_clinic boolean,
    visiting_hours character varying(255) COLLATE pg_catalog."default",
    clinic_visiting_hours character varying(255) COLLATE pg_catalog."default",
    land_line_no character varying(255) COLLATE pg_catalog."default",
    
    salutation character varying(255) COLLATE pg_catalog."default",
    job_title character varying(255) COLLATE pg_catalog."default",
    default_contact boolean,
    sales_leads_id integer,
    sales_customers_id integer,
    CONSTRAINT sales_contacts_pkey PRIMARY KEY (id),
    CONSTRAINT fk_sales_customers FOREIGN KEY (sales_customers_id)
        REFERENCES public.sales_customers (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT fk_sales_leads FOREIGN KEY (sales_leads_id)
        REFERENCES public.sales_leads (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT fk_user_created FOREIGN KEY (user_created)
        REFERENCES public.sales_employees (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT fk_user_updated FOREIGN KEY (user_updated)
        REFERENCES public.sales_employees (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.sales_contacts
    OWNER to postgres;


-- Table: public.sales_addresses

-- DROP TABLE IF EXISTS public.sales_addresses;
CREATE TABLE IF NOT EXISTS public.sales_addresses (
    id SERIAL PRIMARY KEY NOT NULL,
    user_created INTEGER,
    date_created TIMESTAMP,
    user_updated INTEGER,
    date_updated TIMESTAMP,
    contact_name VARCHAR(255),
    type VARCHAR(255),
    isactive BOOLEAN NOT NULL DEFAULT FALSE,
    block VARCHAR(255),
    department VARCHAR(255),
    door_no VARCHAR(255),
    street VARCHAR(255),
    landmark VARCHAR(255),
    to_communication BOOLEAN,
    default_address BOOLEAN,
    sales_leads_id INTEGER,
    territory_id INTEGER,
    area_id INTEGER,
    city_id INTEGER,
    pincode_id INTEGER,
    city_of_referral_id INTEGER,
    district_id INTEGER,
    state_id INTEGER,
    
    CONSTRAINT fk_sales_lead_territory FOREIGN KEY (territory_id) REFERENCES sales_territories(id),
    CONSTRAINT fk_sales_lead_area FOREIGN KEY (area_id) REFERENCES sales_areas(id),
    CONSTRAINT fk_sales_lead_city FOREIGN KEY (city_id) REFERENCES sales_cities(id),
    CONSTRAINT fk_sales_lead_pincode FOREIGN KEY (pincode_id) REFERENCES pincodes(id),
    CONSTRAINT fk_sales_lead_city_of_referral FOREIGN KEY (city_of_referral_id) REFERENCES sales_cities(id),
    CONSTRAINT fk_sales_lead_district FOREIGN KEY (district_id) REFERENCES sales_districts(id),
    CONSTRAINT fk_sales_lead_state FOREIGN KEY (state_id) REFERENCES sales_states(id),
    CONSTRAINT fk_sales_leads FOREIGN KEY (sales_leads_id) REFERENCES public.sales_leads (id),
    CONSTRAINT fk_user_created FOREIGN KEY (user_created) REFERENCES public.sales_employees (id),
    CONSTRAINT fk_user_updated FOREIGN KEY (user_updated) REFERENCES public.sales_employees (id)
);

ALTER TABLE IF EXISTS public.sales_addresses OWNER TO postgres;



-- Table: public.sales_leads_business_challenges

-- DROP TABLE IF EXISTS public.sales_leads_business_challenges;

CREATE TABLE IF NOT EXISTS public.sales_leads_business_challenges
(
    id integer NOT NULL DEFAULT nextval('sales_leads_business_challenges_id_seq'::regclass),
    user_created integer,
    date_created timestamp without time zone,
    user_updated integer,
    date_updated timestamp without time zone,
    solution text COLLATE pg_catalog."default",
    challenges text COLLATE pg_catalog."default",
    isactive boolean DEFAULT false,
    sales_leads_id integer,
    solution_product_ids text,
    solution_products text,
    CONSTRAINT sales_leads_business_challenges_pkey PRIMARY KEY (id),
    CONSTRAINT fk_sales_leads FOREIGN KEY (sales_leads_id)
        REFERENCES public.sales_leads (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT fk_user_created FOREIGN KEY (user_created)
        REFERENCES public.sales_employees (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT fk_user_updated FOREIGN KEY (user_updated)
        REFERENCES public.sales_employees (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.sales_leads_business_challenges
    OWNER to postgres;


-- Table: public.sales_lead_solution_products

-- DROP TABLE IF EXISTS public.sales_lead_solution_products;

CREATE TABLE IF NOT EXISTS public.sales_lead_solution_products
(
    id integer NOT NULL DEFAULT nextval('sales_lead_solution_products_id_seq'::regclass),
    user_created integer,
    date_created timestamp without time zone,
    user_updated integer,
    date_updated timestamp without time zone,
    inventory_items_id integer,
    sales_lead_business_challenges_id integer,
    CONSTRAINT sales_lead_solution_products_pkey PRIMARY KEY (id),
    CONSTRAINT fk_inventory_items FOREIGN KEY (inventory_items_id)
        REFERENCES public.inventory_items (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT fk_sales_business_challenges FOREIGN KEY (sales_lead_business_challenges_id)
        REFERENCES public.sales_leads_business_challenges (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT fk_user_created FOREIGN KEY (user_created)
        REFERENCES public.sales_employees (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT fk_user_updated FOREIGN KEY (user_updated)
        REFERENCES public.sales_employees (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.sales_lead_solution_products
    OWNER to postgres;


-- Table: public.sales_products

-- DROP TABLE IF EXISTS public.sales_products;

CREATE TABLE IF NOT EXISTS public.sales_products
(
    id integer NOT NULL DEFAULT nextval('sales_products_id_seq'::regclass),
    user_created integer,
    date_created timestamp without time zone,
    user_updated integer,
    date_updated timestamp without time zone,
    qty integer,
    amount double precision,
    isactive boolean DEFAULT false,
    inventory_items_id integer,
    CONSTRAINT sales_products_pkey PRIMARY KEY (id),
    CONSTRAINT fk_inventory_items FOREIGN KEY (inventory_items_id)
        REFERENCES public.inventory_items (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT fk_user_created FOREIGN KEY (user_created)
        REFERENCES public.sales_employees (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT fk_user_updated FOREIGN KEY (user_updated)
        REFERENCES public.sales_employees (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.sales_products
    OWNER to postgres;
