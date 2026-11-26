USE InventorySalesDB;
GO
/* Development accounts only. The passwords are shared and must not be used in production. */
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserName = N'admin')
    INSERT dbo.Users (FullName, UserName, PasswordHash, Role)
    VALUES (N'Test Administrator', N'admin', N'AQAAAAIAAYagAAAAEFrlINMC80SsAtQEq7FLlcX/cBG5Bh4Lz3bnf8nsDtUlqyX4+iTyFA2+lSK6afre1A==', N'Administrator');
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserName = N'sales')
    INSERT dbo.Users (FullName, UserName, PasswordHash, Role)
    VALUES (N'Test Sales Staff', N'sales', N'AQAAAAIAAYagAAAAEFaRthoB56gfb3tXlQVWS8GKj/dmetxdzrzp1kKX+GdbIRqv2XvTUGPaB5BsZisLZA==', N'Sales Staff');
COMMIT;
GO
