USE InventorySalesDB;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
/* Month-to-date estimates based on sales, batch purchase costs and expired stock remaining. */
CREATE OR ALTER PROCEDURE dbo.usp_Dashboard_FinancialSummary
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Today date = CONVERT(date, GETDATE());
    DECLARE @Start date = DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1);
    DECLARE @Sales decimal(19,2), @Cost decimal(19,2), @Expired decimal(19,2), @Missing int;

    SELECT @Sales = COALESCE(SUM(CONVERT(decimal(19,2), TotalAmount)), 0)
    FROM dbo.Sales
    WHERE SaleDate >= @Start AND SaleDate < DATEADD(day, 1, @Today);

    SELECT @Cost = COALESCE(SUM(CONVERT(decimal(19,2), allocation.QuantityUsed) * batch.UnitCost), 0)
    FROM dbo.Sales sale
    JOIN dbo.SaleItems item ON item.SaleId = sale.SaleId
    JOIN dbo.SaleItemBatches allocation ON allocation.SaleItemId = item.SaleItemId
    JOIN dbo.StockBatches batch ON batch.StockBatchId = allocation.StockBatchId
    WHERE sale.SaleDate >= @Start AND sale.SaleDate < DATEADD(day, 1, @Today);

    /* An incomplete allocation means the recorded purchase cost is not complete. */
    SELECT @Missing = COUNT(*)
    FROM dbo.Sales sale
    JOIN dbo.SaleItems item ON item.SaleId = sale.SaleId
    OUTER APPLY (SELECT SUM(CONVERT(bigint, QuantityUsed)) AS Allocated
                 FROM dbo.SaleItemBatches WHERE SaleItemId = item.SaleItemId) a
    WHERE sale.SaleDate >= @Start AND sale.SaleDate < DATEADD(day, 1, @Today)
      AND COALESCE(a.Allocated, 0) <> item.Quantity;

    /* A sale without item records has no traceable purchase costs. */
    SELECT @Missing = @Missing + COUNT(*)
    FROM dbo.Sales sale
    WHERE sale.SaleDate >= @Start AND sale.SaleDate < DATEADD(day, 1, @Today)
      AND NOT EXISTS (SELECT 1 FROM dbo.SaleItems item WHERE item.SaleId = sale.SaleId);

    /* Expiry loss is estimated from units still recorded on batches that expired this month. */
    SELECT @Expired = COALESCE(SUM(CONVERT(decimal(19,2), QuantityRemaining) * UnitCost), 0)
    FROM dbo.StockBatches
    WHERE ExpiryDate >= @Start AND ExpiryDate < @Today AND QuantityRemaining > 0;

    SELECT @Start AS PeriodStart, @Today AS AsOfDate, @Sales AS SalesValue,
           @Cost AS SoldStockCost, @Expired AS EstimatedExpiryCost, @Missing AS MissingCostItems;
END;
GO
