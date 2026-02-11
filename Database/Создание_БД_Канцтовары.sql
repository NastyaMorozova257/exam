-- Скрипт для PostgreSQL. Выполнять в pgAdmin: создать БД (например store_db), затем выполнить весь скрипт в Query Tool.
-- Тематика: магазин канцтоваров. 3НФ, ссылочная целостность.

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

-- ---- СХЕМА ----
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

-- ---- ДАННЫЕ: КАНЦТОВАРЫ ----
INSERT INTO "Role" ("Name") VALUES ('Administrator'), ('Manager'), ('Guest'), ('Client');
INSERT INTO "Status" ("Name") VALUES ('Новый'), ('В обработке'), ('Оплачен'), ('Отправлен'), ('Доставлен'), ('Отменён');

-- Логины: chief, staff, anon, buyer. Пароли: P@ssChief1, P@ssStaff2, P@ssAnon3, P@ssBuyer4 (BCrypt)
INSERT INTO "User" ("Login", "PasswordHash", "FullName", "RoleId") VALUES
    ('chief', '$2a$11$8NfQPrA7KG8/DDOb4tgq0ej0Hz4zZ2xNYgtqKebJmNRVDrHHJewia', 'Главный оператор', 1),
    ('staff', '$2a$11$4eiL60IGmNsPaYvkOAPqquJX563kgEurycbV.YAZfA0.pAnuao85e', 'Сотрудник склада', 2),
    ('anon',  '$2a$11$0wXv2ejYmSTtthHVwuuKZOGdZ.hH4P.vUJYxTd61/TblqRurj5OaK', 'Без учётки', 3),
    ('buyer', '$2a$11$yHkbFRzHOLKATyRVXJICc.bX0yWg6khG1GS1p4uvz7InxHM8GAgk2', 'Покупатель', 4);

-- Категории: канцтовары
INSERT INTO "Category" ("Name") VALUES
    ('Тетради и блокноты'),
    ('Письменные принадлежности'),
    ('Бумага и картон'),
    ('Папки и архивация'),
    ('Канцелярские мелочи');

-- Производители
INSERT INTO "Manufacturer" ("Name") VALUES
    ('ООО Сибирская бумага'),
    ('ИП КанцТорг'),
    ('ЗАО Офис-Снаб'),
    ('Тетрадная фабрика №2');

-- Адреса пунктов выдачи
INSERT INTO "Address" ("City", "Street", "Building", "PostalCode") VALUES
    ('Новосибирск', 'ул. Красный проспект', '100', '630099'),
    ('Новосибирск', 'ул. Дуси Ковальчук', '179', '630049'),
    ('Барнаул', 'пр. Ленина', '54', '656002');

-- Поставщики
INSERT INTO "Supplier" ("Name", "Contact", "Phone", "AddressId") VALUES
    ('КанцОпт Сибирь', 'Менеджер Анна', '+7-383-111-22-33', 1),
    ('ОфисМаркет', 'Отдел закупок', '+7-383-444-55-66', 2),
    ('БумагаПлюс', 'Иванова М.И.', '+7-385-777-88-99', 3);

-- Товары: канцтовары
INSERT INTO "Product" ("Name", "Description", "Price", "Quantity", "CategoryId", "ManufacturerId", "SupplierId") VALUES
    ('Тетрадь 48 листов клетка', 'Общая тетрадь, клетка, без полей', 45.00, 200, 1, 4, 1),
    ('Тетрадь 96 листов в линейку', 'Толстая общая тетрадь, линейка', 89.00, 150, 1, 4, 1),
    ('Блокнот А5 80 листов', 'Блокнот для записей, клетка', 120.00, 80, 1, 2, 2),
    ('Ручка шариковая синяя', 'Синяя паста, 0.7 мм', 25.00, 500, 2, 2, 1),
    ('Ручка шариковая чёрная', 'Чёрная паста, 0.5 мм', 28.00, 480, 2, 2, 1),
    ('Карандаш чёрный HB', 'Деревянный карандаш HB', 18.00, 300, 2, 2, 2),
    ('Ластик белый', 'Канцелярский ластик', 15.00, 250, 2, 3, 2),
    ('Бумага А4 500 листов', 'Офисная бумага 80 г/м²', 299.00, 60, 3, 1, 3),
    ('Цветная бумага А4 10 листов', 'Набор цветной бумаги', 55.00, 120, 3, 1, 2),
    ('Картон цветной А4 10 л', 'Набор цветного картона', 75.00, 90, 3, 1, 2),
    ('Папка-регистратор А4', 'Папка на кольцах 70 мм', 185.00, 45, 4, 3, 2),
    ('Папка-скоросшиватель', 'Папка с зажимом А4', 42.00, 180, 4, 3, 1),
    ('Степлер малый', 'Степлер на 20 листов', 165.00, 35, 5, 3, 2),
    ('Скрепки 28 мм коробка', 'Коробка 100 шт', 35.00, 200, 5, 2, 1),
    ('Стикеры 76x76 мм блок', 'Блок липких листочков 100 шт', 89.00, 70, 5, 2, 2);
