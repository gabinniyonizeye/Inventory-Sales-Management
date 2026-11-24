USE InventorySalesDB;
GO

CREATE OR ALTER PROCEDURE dbo.Supplier_GetAll @SearchTerm nvarchar(150)=NULL AS
SELECT SupplierId,SupplierName,Phone,Email,Address,IsActive,CreatedAt
FROM dbo.Suppliers
WHERE NULLIF(TRIM(@SearchTerm),N'') IS NULL OR SupplierName LIKE N'%' + TRIM(@SearchTerm) + N'%' OR Phone LIKE N'%' + TRIM(@SearchTerm) + N'%' OR Email LIKE N'%' + TRIM(@SearchTerm) + N'%'
ORDER BY SupplierName;
GO

CREATE OR ALTER PROCEDURE dbo.Supplier_GetById @SupplierId int AS
SELECT SupplierId,SupplierName,Phone,Email,Address,IsActive,CreatedAt FROM dbo.Suppliers WHERE SupplierId=@SupplierId;
GO

CREATE OR ALTER PROCEDURE dbo.Supplier_Save @SupplierId int=NULL,@SupplierName nvarchar(150),@Phone nvarchar(30)=NULL,@Email nvarchar(150)=NULL,@Address nvarchar(250)=NULL,@IsActive bit=1 AS
BEGIN
 SET NOCOUNT ON;
 SET @SupplierName=TRIM(@SupplierName); SET @Phone=NULLIF(TRIM(@Phone),N''); SET @Email=NULLIF(TRIM(@Email),N''); SET @Address=NULLIF(TRIM(@Address),N'');
 IF NULLIF(@SupplierName,N'') IS NULL THROW 50003,'Supplier name is required.',1;
 IF @SupplierId IS NULL INSERT dbo.Suppliers(SupplierName,Phone,Email,Address,IsActive) VALUES(@SupplierName,@Phone,@Email,@Address,@IsActive);
 ELSE IF NOT EXISTS(SELECT 1 FROM dbo.Suppliers WHERE SupplierId=@SupplierId) THROW 50012,'Supplier was not found.',1;
 ELSE UPDATE dbo.Suppliers SET SupplierName=@SupplierName,Phone=@Phone,Email=@Email,Address=@Address,IsActive=@IsActive WHERE SupplierId=@SupplierId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.StockBatch_GetAll AS
SELECT b.StockBatchId,b.StockReceiptId,b.ProductId,p.ProductName,s.SupplierName,b.QuantityReceived,b.QuantityRemaining,b.UnitCost,b.ExpiryDate,b.CreatedAt
FROM dbo.StockBatches b JOIN dbo.Products p ON p.ProductId=b.ProductId JOIN dbo.StockReceipts r ON r.StockReceiptId=b.StockReceiptId JOIN dbo.Suppliers s ON s.SupplierId=r.SupplierId
ORDER BY CASE WHEN b.ExpiryDate IS NULL THEN 1 ELSE 0 END,b.ExpiryDate,b.StockBatchId;
GO

-- 1. Update Product_GetAll to support Search, Category Filter, and Available Stock
CREATE OR ALTER PROCEDURE dbo.Product_GetAll
    @SearchTerm NVARCHAR(150) = NULL,
    @CategoryId INT = NULL
AS
BEGIN
    SELECT 
        p.ProductId, 
        p.CategoryId,
        p.ProductName, 
        p.SellingPrice, 
        p.Unit, 
        p.ReorderLevel, 
        p.IsActive, 
        c.CategoryName,
        COALESCE(SUM(b.QuantityRemaining), 0) AS AvailableQuantity
    FROM dbo.Products p
    JOIN dbo.Categories c ON c.CategoryId = p.CategoryId
    LEFT JOIN dbo.StockBatches b ON p.ProductId = b.ProductId 
        AND (b.ExpiryDate IS NULL OR b.ExpiryDate >= CAST(GETDATE() AS DATE))
    WHERE 
        (@SearchTerm IS NULL OR p.ProductName LIKE '%' + @SearchTerm + '%')
        AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
    GROUP BY 
        p.ProductId, p.CategoryId, p.ProductName, p.SellingPrice, p.Unit, p.ReorderLevel, p.IsActive, c.CategoryName
    ORDER BY 
        p.ProductName;
END
GO

-- 2. Category_GetAll
CREATE OR ALTER PROCEDURE dbo.Category_GetAll
AS
BEGIN
    SELECT CategoryId, CategoryName, Description, IsActive 
    FROM Categories 
    ORDER BY CategoryName;
END
GO

-- 3. Category_Save (Handles both Insert and Update)
CREATE OR ALTER PROCEDURE dbo.Category_Save
    @CategoryId INT = NULL,
    @CategoryName NVARCHAR(100),
    @Description NVARCHAR(250) = NULL,
    @IsActive BIT
AS
BEGIN
    IF @CategoryId IS NULL OR @CategoryId = 0
    BEGIN
        INSERT INTO Categories (CategoryName, Description, IsActive)
        VALUES (@CategoryName, @Description, @IsActive);
    END
    ELSE
    BEGIN
        UPDATE Categories 
        SET CategoryName = @CategoryName, 
            Description = @Description, 
            IsActive = @IsActive
        WHERE CategoryId = @CategoryId;
    END
END
GO