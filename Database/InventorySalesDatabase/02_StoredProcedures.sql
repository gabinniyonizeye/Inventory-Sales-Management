USE InventorySalesDB;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE OR ALTER PROCEDURE dbo.Category_GetAll AS SELECT * FROM dbo.Categories ORDER BY CategoryName;
GO
CREATE OR ALTER PROCEDURE dbo.Category_Save @CategoryId int = NULL, @CategoryName nvarchar(100), @Description nvarchar(250)=NULL, @IsActive bit=1 AS
BEGIN SET NOCOUNT ON; IF NULLIF(LTRIM(RTRIM(@CategoryName)),N'') IS NULL THROW 50001,'Category name is required.',1; IF @CategoryId IS NULL INSERT dbo.Categories(CategoryName,Description,IsActive) VALUES(TRIM(@CategoryName),@Description,@IsActive); ELSE UPDATE dbo.Categories SET CategoryName=TRIM(@CategoryName),Description=@Description,IsActive=@IsActive WHERE CategoryId=@CategoryId; END;
GO
CREATE OR ALTER PROCEDURE dbo.Product_GetAll AS
SELECT p.*, c.CategoryName, COALESCE(s.Available,0) AvailableQuantity FROM dbo.Products p JOIN dbo.Categories c ON c.CategoryId=p.CategoryId OUTER APPLY (SELECT SUM(CONVERT(bigint,b.QuantityRemaining)) Available FROM dbo.StockBatches b WHERE b.ProductId=p.ProductId AND (b.ExpiryDate IS NULL OR b.ExpiryDate>=CONVERT(date,GETDATE()))) s ORDER BY p.ProductName;
GO
CREATE OR ALTER PROCEDURE dbo.Product_Save @ProductId int=NULL,@CategoryId int,@ProductName nvarchar(150),@SellingPrice decimal(18,2),@ReorderLevel int,@Unit nvarchar(30),@IsActive bit=1 AS
BEGIN SET NOCOUNT ON; IF NULLIF(TRIM(@ProductName),N'') IS NULL OR NULLIF(TRIM(@Unit),N'') IS NULL OR @SellingPrice<0 OR @ReorderLevel<0 THROW 50002,'Invalid product details.',1; IF @ProductId IS NULL INSERT dbo.Products(CategoryId,ProductName,SellingPrice,ReorderLevel,Unit,IsActive) VALUES(@CategoryId,TRIM(@ProductName),@SellingPrice,@ReorderLevel,TRIM(@Unit),@IsActive); ELSE UPDATE dbo.Products SET CategoryId=@CategoryId,ProductName=TRIM(@ProductName),SellingPrice=@SellingPrice,ReorderLevel=@ReorderLevel,Unit=TRIM(@Unit),IsActive=@IsActive WHERE ProductId=@ProductId; END;
GO
/* JSON example: [{"productId":1,"quantity":2},{"productId":2,"quantity":1}] */
CREATE OR ALTER PROCEDURE dbo.Sale_Create @UserId int,@Items nvarchar(max) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF ISJSON(@Items)<>1 OR NOT EXISTS(SELECT 1 FROM OPENJSON(@Items)) THROW 50007,'Sale items are required.',1;
 DECLARE @raw TABLE(ProductId int,Quantity int);
 INSERT @raw SELECT ProductId,Quantity FROM OPENJSON(@Items) WITH(ProductId int '$.productId',Quantity int '$.quantity');
 IF EXISTS(SELECT 1 FROM @raw WHERE ProductId IS NULL OR Quantity IS NULL OR Quantity<=0) THROW 50008,'Invalid sale item.',1;
 DECLARE @lines TABLE(ProductId int PRIMARY KEY,Quantity int,Price decimal(18,2));
 INSERT @lines SELECT r.ProductId,SUM(r.Quantity),MAX(p.SellingPrice) FROM @raw r LEFT JOIN dbo.Products p ON p.ProductId=r.ProductId AND p.IsActive=1 GROUP BY r.ProductId;
 IF EXISTS(SELECT 1 FROM @lines WHERE Price IS NULL) THROW 50009,'Product is unavailable.',1;
 BEGIN TRY
 BEGIN TRAN;
 /* Serialize sales to prevent concurrent sales consuming the same batch. */
 DECLARE @lock int; EXEC @lock=sys.sp_getapplock @Resource='InventorySales.Stock',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
 IF @lock<0 THROW 50010,'Unable to lock stock. Please retry.',1;
 IF EXISTS(SELECT 1 FROM @lines l WHERE l.Quantity > (SELECT COALESCE(SUM(CONVERT(bigint,b.QuantityRemaining)),0) FROM dbo.StockBatches b WHERE b.ProductId=l.ProductId AND (b.ExpiryDate IS NULL OR b.ExpiryDate>=CONVERT(date,GETDATE())))) THROW 50011,'Insufficient non-expired stock.',1;
 INSERT dbo.Sales(SoldByUserId,TotalAmount) SELECT @UserId,SUM(Quantity*Price) FROM @lines; DECLARE @sale int=SCOPE_IDENTITY(),@product int,@qty int,@price decimal(18,2),@line int,@batch int,@available int,@take int;
 DECLARE lines CURSOR LOCAL FAST_FORWARD FOR SELECT ProductId,Quantity,Price FROM @lines ORDER BY ProductId; OPEN lines; FETCH NEXT FROM lines INTO @product,@qty,@price;
 WHILE @@FETCH_STATUS=0 BEGIN
 INSERT dbo.SaleItems(SaleId,ProductId,Quantity,UnitPrice) VALUES(@sale,@product,@qty,@price); SET @line=SCOPE_IDENTITY();
 WHILE @qty>0 BEGIN
 SELECT TOP(1) @batch=StockBatchId,@available=QuantityRemaining FROM dbo.StockBatches WHERE ProductId=@product AND QuantityRemaining>0 AND (ExpiryDate IS NULL OR ExpiryDate>=CONVERT(date,GETDATE())) ORDER BY CASE WHEN ExpiryDate IS NULL THEN 1 ELSE 0 END,ExpiryDate,StockBatchId;
 SET @take=CASE WHEN @qty<@available THEN @qty ELSE @available END;
 UPDATE dbo.StockBatches SET QuantityRemaining=QuantityRemaining-@take WHERE StockBatchId=@batch;
 INSERT dbo.SaleItemBatches(SaleItemId,StockBatchId,QuantityUsed) VALUES(@line,@batch,@take); SET @qty-=@take;
 END;
 FETCH NEXT FROM lines INTO @product,@qty,@price; END;
 CLOSE lines; DEALLOCATE lines; COMMIT; SELECT @sale AS SaleId;
 END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE OR ALTER PROCEDURE dbo.Sale_GetAll @FromDate date=NULL,@ToDate date=NULL AS SELECT * FROM dbo.Sales WHERE (@FromDate IS NULL OR SaleDate>=@FromDate) AND (@ToDate IS NULL OR SaleDate<DATEADD(day,1,@ToDate)) ORDER BY SaleDate DESC;
GO
CREATE OR ALTER PROCEDURE dbo.Sale_GetById @SaleId int AS BEGIN SELECT * FROM dbo.Sales WHERE SaleId=@SaleId; SELECT i.*,p.ProductName FROM dbo.SaleItems i JOIN dbo.Products p ON p.ProductId=i.ProductId WHERE i.SaleId=@SaleId; END;
GO
CREATE OR ALTER PROCEDURE dbo.Alert_GetLowStock AS SELECT p.ProductId,p.ProductName,p.ReorderLevel,COALESCE(s.Available,0) AvailableQuantity FROM dbo.Products p OUTER APPLY(SELECT SUM(CONVERT(bigint,b.QuantityRemaining)) Available FROM dbo.StockBatches b WHERE b.ProductId=p.ProductId AND (b.ExpiryDate IS NULL OR b.ExpiryDate>=CONVERT(date,GETDATE()))) s WHERE p.IsActive=1 AND COALESCE(s.Available,0)<=p.ReorderLevel ORDER BY p.ProductName;
GO
CREATE OR ALTER PROCEDURE dbo.Alert_GetNearExpiry @Days int=7 AS SELECT b.*,p.ProductName FROM dbo.StockBatches b JOIN dbo.Products p ON p.ProductId=b.ProductId WHERE b.QuantityRemaining>0 AND b.ExpiryDate BETWEEN CONVERT(date,GETDATE()) AND DATEADD(day,@Days,CONVERT(date,GETDATE())) ORDER BY b.ExpiryDate;
GO
CREATE OR ALTER PROCEDURE dbo.Alert_GetExpired AS SELECT b.*,p.ProductName FROM dbo.StockBatches b JOIN dbo.Products p ON p.ProductId=b.ProductId WHERE b.QuantityRemaining>0 AND b.ExpiryDate<CONVERT(date,GETDATE()) ORDER BY b.ExpiryDate;
GO
CREATE OR ALTER PROCEDURE dbo.Report_GetSalesByDate @FromDate date,@ToDate date AS EXEC dbo.Sale_GetAll @FromDate,@ToDate;
GO
CREATE OR ALTER PROCEDURE dbo.Report_GetInventory AS EXEC dbo.Product_GetAll;
GO
CREATE OR ALTER PROCEDURE dbo.Dashboard_GetSummary AS SELECT COALESCE(SUM(TotalAmount),0) TodaySales,COUNT(*) TodaySaleCount FROM dbo.Sales WHERE SaleDate>=CONVERT(date,GETDATE()) AND SaleDate<DATEADD(day,1,CONVERT(date,GETDATE()));
GO
