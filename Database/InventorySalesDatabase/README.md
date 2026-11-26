# Database setup

For a fresh development database, run these files in SSMS in this order: `01_CreateDatabase.sql`, `02_StoredProcedures.sql`, `05_TestUsers.sql`, then `07_UserProcedures.sql`, `08_Products_Categories_Procedures.sql`, and `09_Suppliers_Stock_Procedures.sql`, `10_Sales_Dashboard_Reports.sql`, and optionally `03_SampleData.sql` once. Run 08 after 02 because it updates product and category procedures. The sample receipt uses the test administrator account. Do not run table creation against existing tables.

Users.UserId is an automatically generated integer. Receipts and sales reference Users. The two shared development accounts are admin / AdminTest!2026 and sales / StaffTest!2026. Their passwords are hashed and are public test credentials; do not use them in production. The application uses cookie login and administrator-only user management. Use ASP.NET Core PasswordHasher to verify the stored hashes.

CreatedAt uses the SQL Server local time. Keep local connection settings out of GitHub.



