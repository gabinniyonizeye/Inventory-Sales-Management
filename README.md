# Inventory and Sales Management

An ASP.NET Core Razor Pages project for managing products, stock, and sales. Login and administrator-only user management are available; inventory and sales features are in progress. The project targets .NET 10.

## Setup

1. Install the .NET 10 SDK, Visual Studio with ASP.NET and web development, SQL Server 2017 or newer, and SSMS.
2. Follow `Database/InventorySalesDatabase/README.md` to create a fresh local database and run the required scripts.
3. Copy `InventorySalesManagement/appsettings.Development.example.json` to `InventorySalesManagement/appsettings.Development.json`. Replace `YOUR_SQL_SERVER` with your own server name. Keep this local file and any passwords out of GitHub.
4. Open `InventorySalesManagement/InventorySalesManagement.slnx` in Visual Studio, restore packages, and run the project.

The database README includes development test accounts. They are shared test credentials and must not be used for real deployments. The app is still under development.

## Collaboration

Work on a feature branch and open a pull request when your work is ready. Include any stored procedure changes with your feature and tell the team which scripts to run. Pulling SQL scripts from GitHub does not automatically update a local database.
