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
    Description NVARCHAR(500) NULL,

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


-- =========================================================
USE CreationStoreDb;
GO

-- =====================================================
-- 1. Sửa bảng Orders
-- Đổi Orders.Status:
-- Processing -> PendingPayment
-- Thêm Orders.PaymentStatus
-- =====================================================

-- 1.1 Xóa CHECK constraint cũ của Orders.Status
IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_Orders_Status'
)
BEGIN
    ALTER TABLE Orders
    DROP CONSTRAINT CK_Orders_Status;
END
GO

-- 1.2 Xóa DEFAULT constraint cũ của Orders.Status
DECLARE @DefaultConstraintName NVARCHAR(200);

SELECT @DefaultConstraintName = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c
    ON dc.parent_object_id = c.object_id
    AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID('Orders')
  AND c.name = 'Status';

IF @DefaultConstraintName IS NOT NULL
BEGIN
    EXEC('ALTER TABLE Orders DROP CONSTRAINT ' + @DefaultConstraintName);
END
GO

-- 1.3 Đổi độ dài cột Status cho rộng hơn
ALTER TABLE Orders
ALTER COLUMN Status VARCHAR(30) NOT NULL;
GO

-- 1.4 Update dữ liệu cũ
UPDATE Orders
SET Status = 'PendingPayment'
WHERE Status = 'Processing';
GO

-- 1.5 Thêm DEFAULT mới cho Orders.Status
ALTER TABLE Orders
ADD CONSTRAINT DF_Orders_Status
DEFAULT 'PendingPayment' FOR Status;
GO

-- 1.6 Thêm CHECK constraint mới cho Orders.Status
ALTER TABLE Orders
ADD CONSTRAINT CK_Orders_Status
CHECK (Status IN ('PendingPayment', 'Paid', 'Completed', 'Cancelled'));
GO


-- =====================================================
-- 2. Thêm PaymentStatus vào Orders
-- =====================================================

IF COL_LENGTH('Orders', 'PaymentStatus') IS NULL
BEGIN
    ALTER TABLE Orders
    ADD PaymentStatus VARCHAR(30) NOT NULL
        CONSTRAINT DF_Orders_PaymentStatus DEFAULT 'Pending';
END
GO

-- 2.1 Update PaymentStatus theo Status hiện tại
UPDATE Orders
SET PaymentStatus =
    CASE
        WHEN Status = 'PendingPayment' THEN 'Pending'
        WHEN Status = 'Paid' THEN 'Succeeded'
        WHEN Status = 'Completed' THEN 'Succeeded'
        WHEN Status = 'Cancelled' THEN 'Cancelled'
        ELSE 'Pending'
    END;
GO

-- 2.2 Thêm CHECK constraint cho PaymentStatus
IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_Orders_PaymentStatus'
)
BEGIN
    ALTER TABLE Orders
    ADD CONSTRAINT CK_Orders_PaymentStatus
    CHECK (PaymentStatus IN ('Pending', 'Succeeded', 'Failed', 'Cancelled'));
END
GO


-- =====================================================
-- 3. Tạo bảng PaymentTransactions
-- =====================================================

IF OBJECT_ID('PaymentTransactions', 'U') IS NULL
BEGIN
    CREATE TABLE PaymentTransactions (
        PaymentTransactionId INT IDENTITY(1,1) PRIMARY KEY,

        OrderId INT NOT NULL,

        PaymentMethod VARCHAR(50) NOT NULL
            CONSTRAINT DF_PaymentTransactions_PaymentMethod DEFAULT 'VNPAY',

        Amount DECIMAL(18,2) NOT NULL,

        TransactionStatus VARCHAR(30) NOT NULL
            CONSTRAINT DF_PaymentTransactions_TransactionStatus DEFAULT 'Pending',

        -- Mã giao dịch do hệ thống mình tạo gửi sang VNPAY
        VnpTxnRef VARCHAR(100) NOT NULL,

        -- Mã giao dịch VNPAY trả về
        VnpTransactionNo VARCHAR(100) NULL,

        -- Mã phản hồi từ VNPAY
        VnpResponseCode VARCHAR(20) NULL,

        -- Trạng thái giao dịch từ VNPAY
        VnpTransactionStatus VARCHAR(20) NULL,

        -- Mã ngân hàng
        VnpBankCode VARCHAR(50) NULL,

        -- Thời gian thanh toán VNPAY trả về
        VnpPayDate VARCHAR(20) NULL,

        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_PaymentTransactions_CreatedAt DEFAULT SYSDATETIME(),

        PaidAt DATETIME2 NULL,

        -- Lưu raw response/callback từ VNPAY để debug
        RawResponse NVARCHAR(MAX) NULL,

        CONSTRAINT FK_PaymentTransactions_Orders
            FOREIGN KEY (OrderId) REFERENCES Orders(OrderId),

        CONSTRAINT CK_PaymentTransactions_Amount
            CHECK (Amount >= 0),

        CONSTRAINT CK_PaymentTransactions_Status
            CHECK (TransactionStatus IN ('Pending', 'Succeeded', 'Failed', 'Cancelled'))
    );
END
GO


-- =====================================================
-- 4. Tạo index cho PaymentTransactions
-- =====================================================

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_PaymentTransactions_OrderId'
      AND object_id = OBJECT_ID('PaymentTransactions')
)
BEGIN
    CREATE INDEX IX_PaymentTransactions_OrderId
    ON PaymentTransactions(OrderId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_PaymentTransactions_VnpTxnRef'
      AND object_id = OBJECT_ID('PaymentTransactions')
)
BEGIN
    CREATE UNIQUE INDEX IX_PaymentTransactions_VnpTxnRef
    ON PaymentTransactions(VnpTxnRef);
END
GO


-- =====================================================
-- 5. Kiểm tra kết quả
-- =====================================================

SELECT 
    OrderId,
    UserId,
    TotalAmount,
    Status,
    PaymentStatus,
    OrderDate
FROM Orders;
GO

SELECT *
FROM PaymentTransactions;
GO