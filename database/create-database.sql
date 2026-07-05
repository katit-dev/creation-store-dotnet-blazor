CREATE DATABASE CreationStoreDb;
GO

USE CreationStoreDb;
GO

-- =========================
-- 1. Users
-- =========================
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username VARCHAR(50) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Email VARCHAR(100) NULL,
    Phone VARCHAR(20) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2 NULL
);
GO

CREATE UNIQUE INDEX IX_Users_Email
ON Users(Email)
WHERE Email IS NOT NULL;
GO

-- =========================
-- 2. Roles
-- =========================
CREATE TABLE Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName VARCHAR(50) NOT NULL UNIQUE
);
GO

-- =========================
-- 3. UserRoles
-- =========================
CREATE TABLE UserRoles (
    UserId INT NOT NULL,
    RoleId INT NOT NULL,

    PRIMARY KEY (UserId, RoleId),

    CONSTRAINT FK_UserRoles_Users
        FOREIGN KEY (UserId) REFERENCES Users(UserId)
        ON DELETE CASCADE,

    CONSTRAINT FK_UserRoles_Roles
        FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
        ON DELETE CASCADE
);
GO

-- =========================
-- 4. Categories
-- =========================
CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

-- =========================
-- 5. Products
-- =========================
CREATE TABLE Products (
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(150) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Price DECIMAL(18,2) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    ImageUrl VARCHAR(255) NULL,
    ValidityDays INT NULL,
    CategoryId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2 NULL,

    CONSTRAINT CK_Products_Price
        CHECK (Price >= 0),

    CONSTRAINT CK_Products_ValidityDays
        CHECK (ValidityDays IS NULL OR ValidityDays >= 0),

    CONSTRAINT FK_Products_Categories
        FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId)
);
GO

CREATE INDEX IX_Products_CategoryId
ON Products(CategoryId);
GO

-- =========================
-- 6. Carts
-- =========================
CREATE TABLE Carts (
    CartId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'Active',
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt DATETIME2 NULL,

    CONSTRAINT CK_Carts_Status
        CHECK (Status IN ('Active', 'CheckedOut', 'Cancelled')),

    CONSTRAINT FK_Carts_Users
        FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

CREATE INDEX IX_Carts_UserId
ON Carts(UserId);
GO

-- Mỗi user chỉ có 1 cart Active
CREATE UNIQUE INDEX IX_Carts_User_Active
ON Carts(UserId)
WHERE Status = 'Active';
GO

-- =========================
-- 7. CartItems
-- =========================
CREATE TABLE CartItems (
    CartItemId INT IDENTITY(1,1) PRIMARY KEY,
    CartId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    PriceAtTime DECIMAL(18,2) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT CK_CartItems_Quantity
        CHECK (Quantity > 0),

    CONSTRAINT CK_CartItems_PriceAtTime
        CHECK (PriceAtTime >= 0),

    CONSTRAINT FK_CartItems_Carts
        FOREIGN KEY (CartId) REFERENCES Carts(CartId)
        ON DELETE CASCADE,

    CONSTRAINT FK_CartItems_Products
        FOREIGN KEY (ProductId) REFERENCES Products(ProductId),

    CONSTRAINT UQ_CartItems_Cart_Product
        UNIQUE (CartId, ProductId)
);
GO

CREATE INDEX IX_CartItems_CartId
ON CartItems(CartId);
GO

CREATE INDEX IX_CartItems_ProductId
ON CartItems(ProductId);
GO

-- =========================
-- 8. Orders
-- =========================
CREATE TABLE Orders (
    OrderId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'Processing',
    OrderDate DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Note NVARCHAR(500) NULL,
    CancelledAt DATETIME2 NULL,
    CancelReason NVARCHAR(500) NULL,

    CONSTRAINT CK_Orders_TotalAmount
        CHECK (TotalAmount >= 0),

    CONSTRAINT CK_Orders_Status
        CHECK (Status IN ('Processing', 'Paid', 'Completed', 'Cancelled')),

    CONSTRAINT FK_Orders_Users
        FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
GO

CREATE INDEX IX_Orders_UserId
ON Orders(UserId);
GO

-- =========================
-- 9. OrderItems
-- =========================
CREATE TABLE OrderItems (
    OrderItemId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    ProductName NVARCHAR(150) NOT NULL,
    Quantity INT NOT NULL,
    PriceAtTime DECIMAL(18,2) NOT NULL,

    CONSTRAINT CK_OrderItems_Quantity
        CHECK (Quantity > 0),

    CONSTRAINT CK_OrderItems_PriceAtTime
        CHECK (PriceAtTime >= 0),

    CONSTRAINT FK_OrderItems_Orders
        FOREIGN KEY (OrderId) REFERENCES Orders(OrderId)
        ON DELETE CASCADE,

    CONSTRAINT FK_OrderItems_Products
        FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);
GO

CREATE INDEX IX_OrderItems_OrderId
ON OrderItems(OrderId);
GO

CREATE INDEX IX_OrderItems_ProductId
ON OrderItems(ProductId);
GO