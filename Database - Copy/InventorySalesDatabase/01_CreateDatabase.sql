/* Run once in SSMS against master. Do not rerun on a database containing data. */
IF DB_ID(N'InventorySalesDB') IS NULL CREATE DATABASE InventorySalesDB;
GO
USE InventorySalesDB;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
GO
CREATE TABLE dbo.Users (UserId int IDENTITY(1,1) NOT NULL PRIMARY KEY, FullName nvarchar(150) NOT NULL, UserName nvarchar(100) NOT NULL UNIQUE, PasswordHash nvarchar(500) NOT NULL, Role nvarchar(20) NOT NULL CHECK(Role IN (N'Administrator',N'Sales Staff')), IsActive bit NOT NULL DEFAULT 1, CreatedAt datetime2 NOT NULL DEFAULT GETDATE());
CREATE TABLE dbo.Categories (CategoryId int IDENTITY PRIMARY KEY, CategoryName nvarchar(100) NOT NULL UNIQUE, Description nvarchar(250) NULL, IsActive bit NOT NULL DEFAULT 1, CreatedAt datetime2 NOT NULL DEFAULT GETDATE());
CREATE TABLE dbo.Products (ProductId int IDENTITY PRIMARY KEY, CategoryId int NOT NULL REFERENCES dbo.Categories(CategoryId), ProductName nvarchar(150) NOT NULL, SellingPrice decimal(18,2) NOT NULL CHECK (SellingPrice>=0), ReorderLevel int NOT NULL DEFAULT 0 CHECK(ReorderLevel>=0), Unit nvarchar(30) NOT NULL, IsActive bit NOT NULL DEFAULT 1, CreatedAt datetime2 NOT NULL DEFAULT GETDATE());
CREATE TABLE dbo.Suppliers (SupplierId int IDENTITY PRIMARY KEY, SupplierName nvarchar(150) NOT NULL, Phone nvarchar(30) NULL, Email nvarchar(150) NULL, Address nvarchar(250) NULL, IsActive bit NOT NULL DEFAULT 1, CreatedAt datetime2 NOT NULL DEFAULT GETDATE());
/* Each receipt and sale belongs to a user. */
CREATE TABLE dbo.StockReceipts (StockReceiptId int IDENTITY PRIMARY KEY, SupplierId int NOT NULL REFERENCES dbo.Suppliers(SupplierId), ReceivedByUserId int NOT NULL REFERENCES dbo.Users(UserId), ReceivedAt datetime2 NOT NULL DEFAULT GETDATE(), Notes nvarchar(250) NULL, CreatedAt datetime2 NOT NULL DEFAULT GETDATE());
CREATE TABLE dbo.StockBatches (StockBatchId int IDENTITY PRIMARY KEY, StockReceiptId int NOT NULL REFERENCES dbo.StockReceipts(StockReceiptId), ProductId int NOT NULL REFERENCES dbo.Products(ProductId), QuantityReceived int NOT NULL CHECK(QuantityReceived>0), QuantityRemaining int NOT NULL, UnitCost decimal(18,2) NOT NULL CHECK(UnitCost>=0), ExpiryDate date NULL, CreatedAt datetime2 NOT NULL DEFAULT GETDATE(), CONSTRAINT CK_StockBatches_QuantityRemaining CHECK(QuantityRemaining>=0 AND QuantityRemaining<=QuantityReceived));
CREATE TABLE dbo.Sales (SaleId int IDENTITY PRIMARY KEY, SoldByUserId int NOT NULL REFERENCES dbo.Users(UserId), SaleDate datetime2 NOT NULL DEFAULT GETDATE(), TotalAmount decimal(18,2) NOT NULL CHECK(TotalAmount>=0), CreatedAt datetime2 NOT NULL DEFAULT GETDATE());
CREATE TABLE dbo.SaleItems (SaleItemId int IDENTITY PRIMARY KEY, SaleId int NOT NULL REFERENCES dbo.Sales(SaleId), ProductId int NOT NULL REFERENCES dbo.Products(ProductId), Quantity int NOT NULL CHECK(Quantity>0), UnitPrice decimal(18,2) NOT NULL CHECK(UnitPrice>=0), LineTotal AS CONVERT(decimal(18,2),Quantity*UnitPrice) PERSISTED, CreatedAt datetime2 NOT NULL DEFAULT GETDATE());
CREATE TABLE dbo.SaleItemBatches (SaleItemBatchId int IDENTITY PRIMARY KEY, SaleItemId int NOT NULL REFERENCES dbo.SaleItems(SaleItemId), StockBatchId int NOT NULL REFERENCES dbo.StockBatches(StockBatchId), QuantityUsed int NOT NULL CHECK(QuantityUsed>0), CreatedAt datetime2 NOT NULL DEFAULT GETDATE());
CREATE INDEX IX_StockBatches_Product_Expiry ON dbo.StockBatches(ProductId,ExpiryDate) INCLUDE(QuantityRemaining);
CREATE INDEX IX_Sales_Date ON dbo.Sales(SaleDate);
GO


