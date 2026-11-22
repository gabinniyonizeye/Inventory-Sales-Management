USE InventorySalesDB;
GO
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF EXISTS (SELECT 1 FROM dbo.StockReceipts r LEFT JOIN dbo.Users u ON u.UserId=r.ReceivedByUserId WHERE u.UserId IS NULL AND r.ReceivedByUserId<>N'DEMO-USER-NOT-AN-IDENTITY-ACCOUNT') THROW 50102, 'A receipt refers to an unknown user.', 1;
 IF EXISTS (SELECT 1 FROM dbo.Sales s LEFT JOIN dbo.Users u ON u.UserId=s.SoldByUserId WHERE u.UserId IS NULL) THROW 50103, 'A sale refers to an unknown user.', 1;
 IF EXISTS (SELECT 1 FROM dbo.StockReceipts WHERE ReceivedByUserId=N'DEMO-USER-NOT-AN-IDENTITY-ACCOUNT') AND NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserName=N'admin') THROW 50104, 'An admin account is required for the sample receipt.', 1;
 CREATE TABLE dbo.UsersNumeric (UserId int IDENTITY(1,1) NOT NULL PRIMARY KEY, FullName nvarchar(150) NOT NULL, UserName nvarchar(100) NOT NULL UNIQUE, PasswordHash nvarchar(500) NOT NULL, Role nvarchar(20) NOT NULL CHECK(Role IN (N'Administrator',N'Sales Staff')), IsActive bit NOT NULL DEFAULT 1, CreatedAt datetime2 NOT NULL DEFAULT GETDATE());
 INSERT dbo.UsersNumeric(FullName,UserName,PasswordHash,Role,IsActive,CreatedAt) SELECT FullName,UserName,PasswordHash,Role,IsActive,CreatedAt FROM dbo.Users ORDER BY CASE WHEN UserName=N'admin' THEN 0 WHEN UserName=N'sales' THEN 1 ELSE 2 END,UserName;
 UPDATE r SET ReceivedByUserId=CONVERT(nvarchar(450),n.UserId) FROM dbo.StockReceipts r LEFT JOIN dbo.Users old ON old.UserId=r.ReceivedByUserId JOIN dbo.UsersNumeric n ON n.UserName=CASE WHEN r.ReceivedByUserId=N'DEMO-USER-NOT-AN-IDENTITY-ACCOUNT' THEN N'admin' ELSE old.UserName END;
 UPDATE s SET SoldByUserId=CONVERT(nvarchar(450),n.UserId) FROM dbo.Sales s JOIN dbo.Users old ON old.UserId=s.SoldByUserId JOIN dbo.UsersNumeric n ON n.UserName=old.UserName;
 ALTER TABLE dbo.StockReceipts ALTER COLUMN ReceivedByUserId int NOT NULL;
 ALTER TABLE dbo.Sales ALTER COLUMN SoldByUserId int NOT NULL;
 DROP TABLE dbo.Users;
 EXEC sp_rename N'dbo.UsersNumeric', N'Users';
 ALTER TABLE dbo.StockReceipts ADD CONSTRAINT FK_StockReceipts_Users FOREIGN KEY(ReceivedByUserId) REFERENCES dbo.Users(UserId);
 ALTER TABLE dbo.Sales ADD CONSTRAINT FK_Sales_Users FOREIGN KEY(SoldByUserId) REFERENCES dbo.Users(UserId);
 COMMIT;
END TRY
BEGIN CATCH
 IF XACT_STATE() <> 0 ROLLBACK;
 THROW;
END CATCH;
GO
