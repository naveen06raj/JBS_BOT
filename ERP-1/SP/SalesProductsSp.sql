-- Drop existing functions first
DROP FUNCTION IF EXISTS get_sales_products();
DROP FUNCTION IF EXISTS get_sales_product_by_id(integer);

CREATE OR REPLACE FUNCTION get_sales_products()
RETURNS TABLE (
    Id INTEGER,
    UserCreated INTEGER,
    DateCreated TIMESTAMP,
    UserUpdated INTEGER,
    DateUpdated TIMESTAMP,
    MakeId INTEGER,
    MakeName VARCHAR(255),
    ModelId INTEGER,
    ModelName VARCHAR(255),
    ProductId INTEGER,
    ProductName VARCHAR(255),
    CategoryId INTEGER,
    CategoryName VARCHAR(255),
    ItemCode VARCHAR(255),
    ItemName VARCHAR(255),
    Qty INTEGER,
    Amount FLOAT,
    Stage text,
    StageItemId bigint
) 
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        sp.id,
        sp.user_created,
        sp.date_created,
        sp.user_updated,
        sp.date_updated,
        m.id,
        COALESCE(m.name, ''),
        md.id,
        COALESCE(md.name, ''),
        p.id,
        COALESCE(p.name, ''),
        c.id,
        COALESCE(c.name, ''),
        COALESCE(i.item_code, ''),
        COALESCE(i.item_name, ''),
        sp.qty,
        sp.amount,
        sp.stage,
        sp.stage_item_id
    FROM sales_products sp
        LEFT JOIN inventory_items i ON sp.inventory_items_id = i.id
        LEFT JOIN makes m ON i.make_id = m.id
        LEFT JOIN models md ON i.model_id = md.id
        LEFT JOIN products p ON i.product_id = p.id
        LEFT JOIN categories c ON i.category_id = c.id
    WHERE sp.isactive IS TRUE;
END;
$$;


CREATE OR REPLACE PROCEDURE sp_insert_sales_product(
    p_user_id INTEGER,
    p_qty INTEGER,
    p_amount FLOAT,
    p_inventory_items_id INTEGER,
    p_stage VARCHAR(255),
    p_stage_item_id BIGINT
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO sales_products (
        user_created,
        date_created,
        qty,
        amount,
        isactive,
        inventory_items_id,
        stage,
        stage_item_id
    ) VALUES (
        p_user_id,
        CURRENT_TIMESTAMP,
        p_qty,
        p_amount,
        true,
        p_inventory_items_id,
        p_stage,
        p_stage_item_id
    );
END;
$$;

CREATE OR REPLACE PROCEDURE sp_update_sales_product(
    p_id INTEGER,
    p_user_id INTEGER,
    p_qty INTEGER,
    p_amount FLOAT,
    p_inventory_items_id INTEGER,
    p_stage VARCHAR(255),
    p_stage_item_id BIGINT
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE sales_products SET
        user_updated = p_user_id,
        date_updated = CURRENT_TIMESTAMP,
        qty = p_qty,
        amount = p_amount,
        inventory_items_id = p_inventory_items_id,
        stage = p_stage,
        stage_item_id = p_stage_item_id
    WHERE id = p_id;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_delete_sales_product(
    p_id INTEGER,
    p_user_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE sales_products SET
        user_updated = p_user_id,
        date_updated = CURRENT_TIMESTAMP,
        isactive = false
    WHERE id = p_id;
END;
$$;
select * from get_sales_product_by_id(3);
CREATE OR REPLACE FUNCTION get_sales_product_by_id(p_id INTEGER)
RETURNS TABLE (
    Id INTEGER,
    UserCreated INTEGER,
    DateCreated TIMESTAMP,
    UserUpdated INTEGER,
    DateUpdated TIMESTAMP,
    MakeId INTEGER,
    MakeName VARCHAR(255),
    ModelId INTEGER,
    ModelName VARCHAR(255),
    ProductId INTEGER,
    ProductName VARCHAR(255),
    CategoryId INTEGER,
    CategoryName VARCHAR(255),
    ItemCode VARCHAR(255),
    ItemName VARCHAR(255),
    Qty INTEGER,
    Amount FLOAT,
    Stage VARCHAR(255),
    StageItemId BIGINT
) 
LANGUAGE plpgsql
AS $$
#variable_conflict use_column
BEGIN
    RETURN QUERY
    SELECT 
        sp.id as Id,
        sp.user_created as UserCreated,
        sp.date_created as DateCreated,
        sp.user_updated as UserUpdated,
        sp.date_updated as DateUpdated,
        m.id as MakeId,
        COALESCE(m.name, '') as MakeName,
        md.id as ModelId,
        COALESCE(md.name, '') as ModelName,
        p.id as ProductId,
        COALESCE(p.name, '') as ProductName,
        c.id as CategoryId,
        COALESCE(c.name, '') as CategoryName,
        COALESCE(i.item_code, '') as ItemCode,
        COALESCE(i.item_name, '') as ItemName,
        sp.qty as Qty,
        sp.amount as Amount,
        sp.stage as Stage,
        sp.stage_item_id as StageItemId
    FROM sales_products sp
        LEFT JOIN inventory_items i ON sp.inventory_items_id = i.id
        LEFT JOIN makes m ON i.make_id = m.id
        LEFT JOIN models md ON i.model_id = md.id
        LEFT JOIN products p ON i.product_id = p.id
        LEFT JOIN categories c ON i.category_id = c.id
    WHERE sp.id = p_id 
        AND sp.isactive IS TRUE;
END;
$$;

CREATE OR REPLACE FUNCTION get_sales_product_by_stage(
    p_stage VARCHAR(255),
    p_stage_item_id BIGINT
)
RETURNS TABLE (
    Id INTEGER,
    UserCreated INTEGER,
    DateCreated TIMESTAMP,
    UserUpdated INTEGER,
    DateUpdated TIMESTAMP,
    MakeId INTEGER,
    MakeName VARCHAR(255),
    ModelId INTEGER,
    ModelName VARCHAR(255),
    ProductId INTEGER,
    ProductName VARCHAR(255),
    CategoryId INTEGER,
    CategoryName VARCHAR(255),
    ItemCode VARCHAR(255),
    ItemName VARCHAR(255),
    Qty INTEGER,
    Amount FLOAT,
    Stage VARCHAR(255),
    StageItemId BIGINT
) 
LANGUAGE plpgsql
AS $$
#variable_conflict use_column
BEGIN
    RETURN QUERY
    SELECT 
        sp.id as Id,
        sp.user_created as UserCreated,
        sp.date_created as DateCreated,
        sp.user_updated as UserUpdated,
        sp.date_updated as DateUpdated,
        m.id as MakeId,
        COALESCE(m.name, '') as MakeName,
        md.id as ModelId,
        COALESCE(md.name, '') as ModelName,
        p.id as ProductId,
        COALESCE(p.name, '') as ProductName,
        c.id as CategoryId,
        COALESCE(c.name, '') as CategoryName,
        COALESCE(i.item_code, '') as ItemCode,
        COALESCE(i.item_name, '') as ItemName,
        sp.qty as Qty,
        sp.amount as Amount,
        sp.stage as Stage,
        sp.stage_item_id as StageItemId
    FROM sales_products sp
        LEFT JOIN inventory_items i ON sp.inventory_items_id = i.id
        LEFT JOIN makes m ON i.make_id = m.id
        LEFT JOIN models md ON i.model_id = md.id
        LEFT JOIN products p ON i.product_id = p.id
        LEFT JOIN categories c ON i.category_id = c.id    
    WHERE sp.stage = p_stage
        AND sp.stage_item_id = p_stage_item_id
        AND sp.isactive = true 
        AND (i.isactive IS NULL OR i.isactive = true);
END;
$$;
