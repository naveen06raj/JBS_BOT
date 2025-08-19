-- public.sales_addresses definition

-- Drop table

-- DROP TABLE public.sales_addresses;

CREATE TABLE public.sales_addresses (
	id serial4 NOT NULL,
	user_created int4 NULL,
	date_created timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
	user_updated int4 NULL,
	date_updated timestamp NULL,
	contact_name text NULL,
	"type" text NULL,
	city text NULL,
	state text NULL,
	pincode text NULL,
	isactive bool DEFAULT false NOT NULL,
	block text NULL,
	department text NULL,
	area text NULL,
	opportunity_id text NULL,
	door_no text NULL,
	street text NULL,
	land_mark text NULL,
	is_default bool DEFAULT false NULL,
	sales_lead_id int4 NULL,
	CONSTRAINT pk_sales_addresses PRIMARY KEY (id)
);


-- public.sales_addresses foreign keys

ALTER TABLE public.sales_addresses ADD CONSTRAINT fk_sales_addresses_user_created FOREIGN KEY (user_created) REFERENCES public.users(user_id);
ALTER TABLE public.sales_addresses ADD CONSTRAINT fk_sales_addresses_user_updated FOREIGN KEY (user_updated) REFERENCES public.users(user_id);