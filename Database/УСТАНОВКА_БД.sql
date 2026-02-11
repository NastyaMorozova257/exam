-- Скрипт для PostgreSQL. Выполнять в pgAdmin: создать БД trade_db, затем выполнить весь скрипт (Query Tool).

DROP TABLE IF EXISTS "AuditLog"     CASCADE;
DROP TABLE IF EXISTS "OrderItem"     CASCADE;
DROP TABLE IF EXISTS "Order"         CASCADE;
DROP TABLE IF EXISTS "Product"       CASCADE;
DROP TABLE IF EXISTS "Category"     CASCADE;
DROP TABLE IF EXISTS "Supplier"      CASCADE;
DROP TABLE IF EXISTS "Manufacturer" CASCADE;
DROP TABLE IF EXISTS "Address"      CASCADE;
DROP TABLE IF EXISTS "Status"       CASCADE;
DROP TABLE IF EXISTS "User"         CASCADE;
DROP TABLE IF EXISTS "Role"         CASCADE;

-- ---- ЧАСТЬ 1: СХЕМА ----
CREATE TABLE "Role" (
    "Id"    SERIAL PRIMARY KEY,
    "Name"  VARCHAR(50) NOT NULL UNIQUE
);
CREATE TABLE "User" (
    "Id"         SERIAL PRIMARY KEY,
    "Login"      VARCHAR(100) NOT NULL UNIQUE,
    "PasswordHash" VARCHAR(256) NOT NULL,
    "FullName"   VARCHAR(200) NOT NULL,
    "RoleId"     INT NOT NULL REFERENCES "Role"("Id") ON DELETE RESTRICT
);
CREATE TABLE "Address" (
    "Id"       SERIAL PRIMARY KEY,
    "City"     VARCHAR(100) NOT NULL,
    "Street"   VARCHAR(200) NOT NULL,
    "Building" VARCHAR(20)  NOT NULL,
    "Apartment" VARCHAR(20),
    "PostalCode" VARCHAR(20)
);
CREATE TABLE "Status" (
    "Id"   SERIAL PRIMARY KEY,
    "Name" VARCHAR(50) NOT NULL UNIQUE
);
CREATE TABLE "Manufacturer" (
    "Id"   SERIAL PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL
);
CREATE TABLE "Supplier" (
    "Id"         SERIAL PRIMARY KEY,
    "Name"       VARCHAR(200) NOT NULL,
    "Contact"    VARCHAR(200),
    "Phone"      VARCHAR(50),
    "AddressId"  INT REFERENCES "Address"("Id") ON DELETE SET NULL
);
CREATE TABLE "Category" (
    "Id"   SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL UNIQUE
);
CREATE TABLE "Product" (
    "Id"             SERIAL PRIMARY KEY,
    "Name"           VARCHAR(200) NOT NULL,
    "Description"    TEXT,
    "Price"          DECIMAL(18,2) NOT NULL CHECK ("Price" >= 0),
    "Quantity"       INT NOT NULL DEFAULT 0 CHECK ("Quantity" >= 0),
    "CategoryId"     INT NOT NULL REFERENCES "Category"("Id") ON DELETE RESTRICT,
    "ManufacturerId" INT REFERENCES "Manufacturer"("Id") ON DELETE SET NULL,
    "SupplierId"     INT REFERENCES "Supplier"("Id") ON DELETE SET NULL,
    "ImageUrl"       VARCHAR(500)
);
CREATE TABLE "Order" (
    "Id"          SERIAL PRIMARY KEY,
    "UserId"      INT NOT NULL REFERENCES "User"("Id") ON DELETE RESTRICT,
    "StatusId"    INT NOT NULL REFERENCES "Status"("Id") ON DELETE RESTRICT,
    "AddressId"   INT REFERENCES "Address"("Id") ON DELETE SET NULL,
    "CreatedAt"   TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "TotalSum"    DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK ("TotalSum" >= 0)
);
CREATE TABLE "OrderItem" (
    "Id"        SERIAL PRIMARY KEY,
    "OrderId"   INT NOT NULL REFERENCES "Order"("Id") ON DELETE CASCADE,
    "ProductId" INT NOT NULL REFERENCES "Product"("Id") ON DELETE RESTRICT,
    "Quantity"  INT NOT NULL CHECK ("Quantity" > 0),
    "UnitPrice" DECIMAL(18,2) NOT NULL CHECK ("UnitPrice" >= 0),
    UNIQUE ("OrderId", "ProductId")
);
CREATE TABLE "AuditLog" (
    "Id"        SERIAL PRIMARY KEY,
    "UserId"    INT REFERENCES "User"("Id") ON DELETE SET NULL,
    "Action"    VARCHAR(100) NOT NULL,
    "Entity"    VARCHAR(100),
    "EntityId"  INT,
    "Details"   TEXT,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS "IX_Product_Name" ON "Product"("Name");
CREATE INDEX IF NOT EXISTS "IX_Product_CategoryId" ON "Product"("CategoryId");
CREATE INDEX IF NOT EXISTS "IX_Product_Price" ON "Product"("Price");
CREATE INDEX IF NOT EXISTS "IX_Order_UserId" ON "Order"("UserId");
CREATE INDEX IF NOT EXISTS "IX_Order_StatusId" ON "Order"("StatusId");
CREATE INDEX IF NOT EXISTS "IX_Order_CreatedAt" ON "Order"("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_User_Login" ON "User"("Login");

-- ---- ЧАСТЬ 2: НАЧАЛЬНЫЕ ДАННЫЕ (ООО «Обувь») ----
INSERT INTO "Role" ("Name") VALUES ('Administrator'), ('Manager'), ('Guest'), ('Client') ON CONFLICT ("Name") DO NOTHING;
INSERT INTO "Status" ("Name") VALUES ('Новый'), ('В обработке'), ('Оплачен'), ('Отправлен'), ('Доставлен'), ('Отменён') ON CONFLICT ("Name") DO NOTHING;
INSERT INTO "User" ("Login", "PasswordHash", "FullName", "RoleId") VALUES
    ('admin',   '$2a$11$K8A4Am/pd31Hw8EMt2nYI.bk/kuPKQvkCw187.sbIQuPZI9010rEm', 'Администратор', 1),
    ('manager', '$2a$11$gCsHOlQqXm5jrxgEAhjipOhpxM9TSfT/BNHUEYNVTLANLldmikEVO', 'Менеджер Иванов', 2),
    ('guest',   '$2a$11$9/x7bly3lrlk77obq4tT.uB59x9gG6ddfoK.6VVibphAE/nU1yd8W', 'Гость', 3),
    ('client',  '$2a$11$9/x7bly3lrlk77obq4tT.uB59x9gG6ddfoK.6VVibphAE/nU1yd8W', 'Клиент', 4)
ON CONFLICT ("Login") DO NOTHING;
-- Категории для магазина обуви
INSERT INTO "Category" ("Name") VALUES ('Мужская обувь'), ('Женская обувь'), ('Детская обувь'), ('Спортивная обувь') ON CONFLICT ("Name") DO NOTHING;
INSERT INTO "Manufacturer" ("Name") VALUES ('ООО Обувь'), ('ИП Петров'), ('ЗАО Обувная фабрика');
INSERT INTO "Address" ("City", "Street", "Building", "PostalCode") VALUES ('Москва', 'ул. Ленина', '1', '101000'), ('Санкт-Петербург', 'Невский пр.', '10', '190000');
INSERT INTO "Supplier" ("Name", "Contact", "Phone", "AddressId") VALUES ('Поставщик обуви А', 'Иванов', '+7-495-111-22-33', 1), ('Поставщик обуви Б', 'Сидоров', '+7-812-444-55-66', 2);
-- Товары: обувь (ООО «Обувь»)
INSERT INTO "Product" ("Name", "Description", "Price", "Quantity", "CategoryId", "ManufacturerId", "SupplierId") VALUES
    ('Туфли мужские классические', 'Кожаные туфли чёрные, размеры 40-45', 4500.00, 25, 1, 1, 1),
    ('Ботинки зимние', 'Тёплые зимние ботинки на меху', 6200.00, 30, 1, 2, 1),
    ('Кроссовки беговые', 'Лёгкие кроссовки для бега', 3900.00, 40, 4, 1, 2),
    ('Туфли женские на каблуке', 'Туфли-лодочки, каблук 5 см', 3800.00, 35, 2, 1, 2),
    ('Сандалии детские', 'Летние сандалии для детей', 1200.00, 50, 3, 3, 1);

UPDATE "User" SET "PasswordHash" = '$2a$11$K8A4Am/pd31Hw8EMt2nYI.bk/kuPKQvkCw187.sbIQuPZI9010rEm' WHERE "Login" = 'admin';
UPDATE "User" SET "PasswordHash" = '$2a$11$gCsHOlQqXm5jrxgEAhjipOhpxM9TSfT/BNHUEYNVTLANLldmikEVO' WHERE "Login" = 'manager';
UPDATE "User" SET "PasswordHash" = '$2a$11$9/x7bly3lrlk77obq4tT.uB59x9gG6ddfoK.6VVibphAE/nU1yd8W' WHERE "Login" = 'guest';
UPDATE "User" SET "PasswordHash" = '$2a$11$9/x7bly3lrlk77obq4tT.uB59x9gG6ddfoK.6VVibphAE/nU1yd8W' WHERE "Login" = 'client';
