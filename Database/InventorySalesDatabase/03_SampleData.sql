USE InventorySalesDB;
GO
/* Run after scripts 01, 02 and 05 on a fresh development database.
   This data is fictional. Run once; stop if the sample marker already exists. */
SET XACT_ABORT ON;
GO
IF EXISTS (SELECT 1 FROM dbo.Categories WHERE CategoryName = N'Demo Beverages')
    THROW 50100, 'Sample data appears to exist; this script must only be run once.', 1;
GO
BEGIN TRY
 BEGIN TRAN;
 INSERT dbo.Categories(CategoryName, Description) VALUES
 (N'Demo Beverages',N'Sample drinks'),(N'Demo Food',N'Sample food'),(N'Demo Household',N'Sample household items');
 DECLARE @beverages int=(SELECT CategoryId FROM dbo.Categories WHERE CategoryName=N'Demo Beverages'),
         @food int=(SELECT CategoryId FROM dbo.Categories WHERE CategoryName=N'Demo Food'),
         @household int=(SELECT CategoryId FROM dbo.Categories WHERE CategoryName=N'Demo Household');
 INSERT dbo.Products(CategoryId,ProductName,SellingPrice,ReorderLevel,Unit) VALUES
 (@beverages,N'Demo Milk',1500,5,N'bottle'),(@food,N'Demo Rice',3000,5,N'bag'),
 (@food,N'Demo Yogurt',1200,3,N'cup'),(@household,N'Demo Soap',800,4,N'piece');
 INSERT dbo.Suppliers(SupplierName,Phone,Email) VALUES
 (N'Demo General Supplier',N'0780000000',N'supplier@example.com');
 DECLARE @supplier int=SCOPE_IDENTITY();
 DECLARE @adminId int=(SELECT UserId FROM dbo.Users WHERE UserName=N'admin');
 IF @adminId IS NULL THROW 50106, 'Run 05_TestUsers.sql before sample data.', 1;
 INSERT dbo.StockReceipts(SupplierId,ReceivedByUserId,Notes) VALUES(@supplier,@adminId,N'Fictional sample delivery');
 DECLARE @receipt int=SCOPE_IDENTITY(), @today date=CONVERT(date,GETDATE());
 INSERT dbo.StockBatches(StockReceiptId,ProductId,QuantityReceived,QuantityRemaining,UnitCost,ExpiryDate)
 SELECT @receipt,ProductId,10,10,900,DATEADD(day,3,@today) FROM dbo.Products WHERE ProductName=N'Demo Milk' AND CategoryId=@beverages;
 INSERT dbo.StockBatches(StockReceiptId,ProductId,QuantityReceived,QuantityRemaining,UnitCost,ExpiryDate)
 SELECT @receipt,ProductId,3,3,2200,NULL FROM dbo.Products WHERE ProductName=N'Demo Rice' AND CategoryId=@food;
 INSERT dbo.StockBatches(StockReceiptId,ProductId,QuantityReceived,QuantityRemaining,UnitCost,ExpiryDate)
 SELECT @receipt,ProductId,2,2,700,DATEADD(day,-2,@today) FROM dbo.Products WHERE ProductName=N'Demo Yogurt' AND CategoryId=@food;
 INSERT dbo.StockBatches(StockReceiptId,ProductId,QuantityReceived,QuantityRemaining,UnitCost,ExpiryDate)
 SELECT @receipt,ProductId,12,12,500,NULL FROM dbo.Products WHERE ProductName=N'Demo Soap' AND CategoryId=@household;
 COMMIT;
END TRY
BEGIN CATCH
 IF XACT_STATE() <> 0 ROLLBACK;
 THROW;
END CATCH;
GO
/* Test the alert procedures after running 02_StoredProcedures.sql. */
EXEC dbo.Alert_GetLowStock;
EXEC dbo.Alert_GetNearExpiry @Days=7;
EXEC dbo.Alert_GetExpired;
GO
