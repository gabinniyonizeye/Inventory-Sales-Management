USE InventorySalesDB;
GO
CREATE OR ALTER PROCEDURE dbo.User_GetByUsername @UserName nvarchar(100) AS
BEGIN SET NOCOUNT ON; SELECT UserId, FullName, UserName, PasswordHash, Role, IsActive FROM dbo.Users WHERE UserName=@UserName; END;
GO
CREATE OR ALTER PROCEDURE dbo.User_GetById @UserId int AS
BEGIN SET NOCOUNT ON; SELECT UserId, FullName, UserName, Role, IsActive FROM dbo.Users WHERE UserId=@UserId; END;
GO
CREATE OR ALTER PROCEDURE dbo.User_GetAll AS
BEGIN SET NOCOUNT ON; SELECT UserId,FullName,UserName,Role,IsActive,CreatedAt FROM dbo.Users ORDER BY FullName; END;
GO
CREATE OR ALTER PROCEDURE dbo.User_Create @FullName nvarchar(150), @UserName nvarchar(100), @PasswordHash nvarchar(500), @Role nvarchar(20) AS
BEGIN SET NOCOUNT ON; INSERT dbo.Users(FullName,UserName,PasswordHash,Role) VALUES(@FullName,@UserName,@PasswordHash,@Role); END;
GO
CREATE OR ALTER PROCEDURE dbo.User_Update @UserId int,@FullName nvarchar(150),@Role nvarchar(20),@IsActive bit AS
BEGIN SET NOCOUNT ON; UPDATE dbo.Users SET FullName=@FullName, Role=@Role, IsActive=@IsActive WHERE UserId=@UserId; END;
GO
