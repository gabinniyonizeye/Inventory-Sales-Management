USE InventorySalesDB;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
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
/* JSON example: [{"productId":1,"quantity":10,"unitCost":2500,"expiryDate":"2027-01-01"}] */
CREATE OR ALTER PROCEDURE dbo.StockReceipt_Create @SupplierId int,@UserId int,@Items nvarchar(max),@Notes nvarchar(250)=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF ISJSON(@Items)<>1 OR NOT EXISTS(SELECT 1 FROM OPENJSON(@Items)) THROW 50004,'Receipt items are required.',1;
 DECLARE @i TABLE(ProductId int, Quantity int, UnitCost decimal(18,2), ExpiryDate date);
 INSERT @i SELECT ProductId,Quantity,UnitCost,ExpiryDate FROM OPENJSON(@Items) WITH(ProductId int '$.productId',Quantity int '$.quantity',UnitCost decimal(18,2) '$.unitCost',ExpiryDate date '$.expiryDate');
 IF EXISTS(SELECT 1 FROM @i i LEFT JOIN dbo.Products p ON p.ProductId=i.ProductId WHERE p.ProductId IS NULL OR p.IsActive=0 OR i.Quantity IS NULL OR i.Quantity<=0 OR i.UnitCost IS NULL OR i.UnitCost<0) THROW 50005,'Invalid receipt item.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.Suppliers WHERE SupplierId=@SupplierId AND IsActive=1) THROW 50006,'Supplier is unavailable.',1;
 BEGIN TRAN; INSERT dbo.StockReceipts(SupplierId,ReceivedByUserId,Notes) VALUES(@SupplierId,@UserId,@Notes); DECLARE @id int=SCOPE_IDENTITY(); INSERT dbo.StockBatches(StockReceiptId,ProductId,QuantityReceived,QuantityRemaining,UnitCost,ExpiryDate) SELECT @id,ProductId,Quantity,Quantity,UnitCost,ExpiryDate FROM @i; COMMIT; SELECT @id AS StockReceiptId;
END;
GO
CREATE OR ALTER PROCEDURE dbo.StockReceipt_GetAll AS SELECT r.*,s.SupplierName FROM dbo.StockReceipts r JOIN dbo.Suppliers s ON s.SupplierId=r.SupplierId ORDER BY r.ReceivedAt DESC;
GO
CREATE OR ALTER PROCEDURE dbo.StockReceipt_GetById @StockReceiptId int AS BEGIN SELECT r.*,s.SupplierName,u.FullName AS ReceivedByName FROM dbo.StockReceipts r JOIN dbo.Suppliers s ON s.SupplierId=r.SupplierId JOIN dbo.Users u ON u.UserId=r.ReceivedByUserId WHERE r.StockReceiptId=@StockReceiptId; SELECT b.*,p.ProductName FROM dbo.StockBatches b JOIN dbo.Products p ON p.ProductId=b.ProductId WHERE b.StockReceiptId=@StockReceiptId; END;
GO
CREATE OR ALTER PROCEDURE dbo.StockBatch_GetAll AS
SELECT b.StockBatchId,b.StockReceiptId,b.ProductId,p.ProductName,s.SupplierName,b.QuantityReceived,b.QuantityRemaining,b.UnitCost,b.ExpiryDate,b.CreatedAt
FROM dbo.StockBatches b
JOIN dbo.Products p ON p.ProductId=b.ProductId
JOIN dbo.StockReceipts r ON r.StockReceiptId=b.StockReceiptId
JOIN dbo.Suppliers s ON s.SupplierId=r.SupplierId
ORDER BY CASE WHEN b.ExpiryDate IS NULL THEN 1 ELSE 0 END,b.ExpiryDate,b.StockBatchId;
GO
CREATE OR ALTER PROCEDURE dbo.StockBatch_GetByProduct @ProductId int AS SELECT * FROM dbo.StockBatches WHERE ProductId=@ProductId ORDER BY ExpiryDate,StockBatchId;
GO
