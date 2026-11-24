USE InventorySalesDB;
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