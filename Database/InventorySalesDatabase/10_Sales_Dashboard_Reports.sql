/* 10_Sales_Dashboard_Reports.sql
   Owner: Oscar - sales, dashboard alerts and reports.
   Run in SSMS against InventorySalesDB AFTER scripts 01, 02, 05, 07, 08 and 09.
   Safe to re-run (uses CREATE OR ALTER). */
USE InventorySalesDB;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF TYPE_ID(N'dbo.SaleItemList') IS NULL
    CREATE TYPE dbo.SaleItemList AS TABLE (ProductId int NOT NULL, Quantity int NOT NULL);
GO

/* Saves a sale and deducts stock in one transaction.
   Earliest-expiring eligible batch first; batches without expiry last.
   Expired batches are never used. Business errors use error number 50001. */
CREATE OR ALTER PROCEDURE dbo.usp_CreateSale
    @SoldByUserId int,
    @Items dbo.SaleItemList READONLY,
    @SaleId int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        IF EXISTS (SELECT 1 FROM @Items WHERE Quantity <= 0)
            THROW 50001, N'Quantities must be greater than zero.', 1;
        IF NOT EXISTS (SELECT 1 FROM @Items)
            THROW 50001, N'Add at least one product to the sale.', 1;

        BEGIN TRANSACTION;

        CREATE TABLE #Items (ProductId int PRIMARY KEY, Quantity int NOT NULL,
                             UnitPrice decimal(18,2) NOT NULL, ProductName nvarchar(150) NOT NULL);

        INSERT #Items (ProductId, Quantity, UnitPrice, ProductName)
        SELECT i.ProductId, SUM(i.Quantity), p.SellingPrice, p.ProductName
        FROM @Items i
        JOIN dbo.Products p ON p.ProductId = i.ProductId AND p.IsActive = 1
        GROUP BY i.ProductId, p.SellingPrice, p.ProductName;

        IF (SELECT COUNT(DISTINCT ProductId) FROM @Items) <> (SELECT COUNT(*) FROM #Items)
            THROW 50001, N'One or more products do not exist or are inactive.', 1;

        /* Lock the eligible batches so competing sales cannot take the same stock. */
        SELECT b.StockBatchId, b.ProductId, b.QuantityRemaining, b.ExpiryDate
        INTO #Locked
        FROM dbo.StockBatches b WITH (UPDLOCK, HOLDLOCK)
        WHERE b.ProductId IN (SELECT ProductId FROM #Items)
          AND b.QuantityRemaining > 0
          AND (b.ExpiryDate IS NULL OR b.ExpiryDate >= CAST(GETDATE() AS date));

        DECLARE @Msg nvarchar(400);
        SELECT TOP (1) @Msg = CONCAT(N'Insufficient stock for ', i.ProductName,
               N'. Available: ', ISNULL(a.Qty, 0), N', requested: ', i.Quantity, N'.')
        FROM #Items i
        OUTER APPLY (SELECT SUM(l.QuantityRemaining) AS Qty FROM #Locked l WHERE l.ProductId = i.ProductId) a
        WHERE i.Quantity > ISNULL(a.Qty, 0)
        ORDER BY i.ProductName;
        IF @Msg IS NOT NULL THROW 50001, @Msg, 1;

        INSERT dbo.Sales (SoldByUserId, TotalAmount)
        SELECT @SoldByUserId, SUM(Quantity * UnitPrice) FROM #Items;
        SET @SaleId = SCOPE_IDENTITY();

        CREATE TABLE #ItemMap (SaleItemId int NOT NULL, ProductId int NOT NULL);
        INSERT dbo.SaleItems (SaleId, ProductId, Quantity, UnitPrice)
        OUTPUT inserted.SaleItemId, inserted.ProductId INTO #ItemMap (SaleItemId, ProductId)
        SELECT @SaleId, ProductId, Quantity, UnitPrice FROM #Items;

        ;WITH Ordered AS (
            SELECT l.StockBatchId, l.ProductId, l.QuantityRemaining,
                   SUM(l.QuantityRemaining) OVER (
                       PARTITION BY l.ProductId
                       ORDER BY CASE WHEN l.ExpiryDate IS NULL THEN 1 ELSE 0 END, l.ExpiryDate, l.StockBatchId
                       ROWS UNBOUNDED PRECEDING) AS CumQty
            FROM #Locked l)
        SELECT o.StockBatchId, m.SaleItemId,
               CASE WHEN o.CumQty - o.QuantityRemaining >= i.Quantity THEN 0
                    WHEN o.CumQty <= i.Quantity THEN o.QuantityRemaining
                    ELSE i.Quantity - (o.CumQty - o.QuantityRemaining) END AS QtyUsed
        INTO #Alloc
        FROM Ordered o
        JOIN #Items i ON i.ProductId = o.ProductId
        JOIN #ItemMap m ON m.ProductId = o.ProductId;

        INSERT dbo.SaleItemBatches (SaleItemId, StockBatchId, QuantityUsed)
        SELECT SaleItemId, StockBatchId, QtyUsed FROM #Alloc WHERE QtyUsed > 0;

        UPDATE b SET b.QuantityRemaining = b.QuantityRemaining - a.QtyUsed
        FROM dbo.StockBatches b
        JOIN #Alloc a ON a.StockBatchId = b.StockBatchId
        WHERE a.QtyUsed > 0;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Sale_GetAll @FromDate date = NULL, @ToDate date = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.SaleId, s.SaleDate, u.FullName AS SoldBy, s.TotalAmount,
           (SELECT COUNT(*) FROM dbo.SaleItems si WHERE si.SaleId = s.SaleId) AS ItemCount
    FROM dbo.Sales s
    JOIN dbo.Users u ON u.UserId = s.SoldByUserId
    WHERE (@FromDate IS NULL OR s.SaleDate >= @FromDate)
      AND (@ToDate IS NULL OR s.SaleDate < DATEADD(day, 1, @ToDate))
    ORDER BY s.SaleDate DESC, s.SaleId DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Sale_GetDetails @SaleId int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.SaleId, s.SaleDate, u.FullName AS SoldBy, s.TotalAmount
    FROM dbo.Sales s
    JOIN dbo.Users u ON u.UserId = s.SoldByUserId
    WHERE s.SaleId = @SaleId;

    SELECT si.SaleItemId, p.ProductName, si.Quantity, si.UnitPrice, si.LineTotal,
           sib.StockBatchId, sib.QuantityUsed, sb.ExpiryDate
    FROM dbo.SaleItems si
    JOIN dbo.Products p ON p.ProductId = si.ProductId
    LEFT JOIN dbo.SaleItemBatches sib ON sib.SaleItemId = si.SaleItemId
    LEFT JOIN dbo.StockBatches sb ON sb.StockBatchId = sib.StockBatchId
    WHERE si.SaleId = @SaleId
    ORDER BY si.SaleItemId, sb.ExpiryDate;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Dashboard_Summary
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Today date = CAST(GETDATE() AS date);
    SELECT COUNT(*) AS SalesCount, ISNULL(SUM(TotalAmount), 0) AS TotalAmount
    FROM dbo.Sales
    WHERE SaleDate >= @Today AND SaleDate < DATEADD(day, 1, @Today);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Dashboard_Alerts @NearExpiryDays int = 30
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Today date = CAST(GETDATE() AS date);

    /* 1) Low stock: available (non-expired) quantity at or below reorder level */
    SELECT p.ProductId, p.ProductName, p.ReorderLevel,
           ISNULL(SUM(b.QuantityRemaining), 0) AS Available
    FROM dbo.Products p
    LEFT JOIN dbo.StockBatches b ON b.ProductId = p.ProductId
         AND (b.ExpiryDate IS NULL OR b.ExpiryDate >= @Today)
    WHERE p.IsActive = 1
    GROUP BY p.ProductId, p.ProductName, p.ReorderLevel
    HAVING ISNULL(SUM(b.QuantityRemaining), 0) <= p.ReorderLevel
    ORDER BY Available, p.ProductName;

    /* 2) Near-expiry batches that still have stock */
    SELECT b.StockBatchId, p.ProductName, b.QuantityRemaining, b.ExpiryDate,
           DATEDIFF(day, @Today, b.ExpiryDate) AS DaysLeft
    FROM dbo.StockBatches b
    JOIN dbo.Products p ON p.ProductId = b.ProductId
    WHERE b.QuantityRemaining > 0 AND b.ExpiryDate IS NOT NULL
      AND b.ExpiryDate >= @Today AND b.ExpiryDate <= DATEADD(day, @NearExpiryDays, @Today)
    ORDER BY b.ExpiryDate, p.ProductName;

    /* 3) Expired batches that still have stock */
    SELECT b.StockBatchId, p.ProductName, b.QuantityRemaining, b.ExpiryDate,
           DATEDIFF(day, b.ExpiryDate, @Today) AS DaysExpired
    FROM dbo.StockBatches b
    JOIN dbo.Products p ON p.ProductId = b.ProductId
    WHERE b.QuantityRemaining > 0 AND b.ExpiryDate IS NOT NULL AND b.ExpiryDate < @Today
    ORDER BY b.ExpiryDate, p.ProductName;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Report_Sales @FromDate date = NULL, @ToDate date = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @ToDate = ISNULL(@ToDate, CAST(GETDATE() AS date));
    SET @FromDate = ISNULL(@FromDate, DATEADD(day, -30, @ToDate));

    SELECT CAST(SaleDate AS date) AS SaleDay, COUNT(*) AS SalesCount, SUM(TotalAmount) AS TotalAmount
    FROM dbo.Sales
    WHERE SaleDate >= @FromDate AND SaleDate < DATEADD(day, 1, @ToDate)
    GROUP BY CAST(SaleDate AS date)
    ORDER BY SaleDay DESC;

    SELECT p.ProductName, SUM(si.Quantity) AS QuantitySold, SUM(si.LineTotal) AS Revenue
    FROM dbo.SaleItems si
    JOIN dbo.Sales s ON s.SaleId = si.SaleId
    JOIN dbo.Products p ON p.ProductId = si.ProductId
    WHERE s.SaleDate >= @FromDate AND s.SaleDate < DATEADD(day, 1, @ToDate)
    GROUP BY p.ProductName
    ORDER BY Revenue DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Report_Inventory
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Today date = CAST(GETDATE() AS date);
    ;WITH S AS (
        SELECT p.ProductId, p.ProductName, c.CategoryName, p.Unit, p.ReorderLevel,
               ISNULL(SUM(CASE WHEN b.ExpiryDate IS NULL OR b.ExpiryDate >= @Today THEN b.QuantityRemaining END), 0) AS Available,
               ISNULL(SUM(CASE WHEN b.ExpiryDate < @Today THEN b.QuantityRemaining END), 0) AS ExpiredQuantity
        FROM dbo.Products p
        JOIN dbo.Categories c ON c.CategoryId = p.CategoryId
        LEFT JOIN dbo.StockBatches b ON b.ProductId = p.ProductId
        WHERE p.IsActive = 1
        GROUP BY p.ProductId, p.ProductName, c.CategoryName, p.Unit, p.ReorderLevel)
    SELECT ProductName, CategoryName, Unit, ReorderLevel, Available, ExpiredQuantity,
           CASE WHEN Available = 0 THEN N'Out of stock'
                WHEN Available <= ReorderLevel THEN N'Low stock'
                ELSE N'OK' END AS StockStatus
    FROM S
    ORDER BY ProductName;
END
GO
