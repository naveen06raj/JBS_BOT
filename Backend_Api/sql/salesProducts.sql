-- public.sales_products definition

-- Drop table

-- DROP TABLE public.sales_products;

CREATE TABLE public.sales_products (
	id int4 GENERATED ALWAYS AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1 NO CYCLE) NOT NULL,
	user_created int4 NULL,
	date_created timestamp NULL,
	user_updated int4 NULL,
	date_updated timestamp NULL,
	qty int4 NULL,
	amount float8 NULL,
	isactive bool DEFAULT false NULL,
	inventory_items_id int4 NULL,
	stage_item_id int8 NULL,
	stage text NULL,
	unit_price numeric(12, 2) NULL,
	CONSTRAINT sales_products_pkey PRIMARY KEY (id)
);


-- public.sales_products foreign keys

ALTER TABLE public.sales_products ADD CONSTRAINT fk_user_created FOREIGN KEY (user_created) REFERENCES public.sales_employees(id);
ALTER TABLE public.sales_products ADD CONSTRAINT fk_user_updated FOREIGN KEY (user_updated) REFERENCES public.sales_employees(id);