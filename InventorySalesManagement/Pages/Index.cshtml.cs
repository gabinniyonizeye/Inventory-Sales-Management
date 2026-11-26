using System.Data;
using InventorySalesManagement.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InventorySalesManagement.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration configuration;
        public decimal TodaySales { get; set; }
        public int TodayCount { get; set; }
        public decimal WeekSales { get; set; }
        public decimal PreviousWeekSales { get; set; }
        public int LowStockCount { get; set; }
        public int ExpiringCount { get; set; }
        public int ExpiredCount { get; set; }
        public int ProductCount { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal MonthStockCost { get; set; }
        public decimal MonthExpiryCost { get; set; }
        public int MissingCostItems { get; set; }
        public DateTime FinancialStart { get; set; }
        public DateTime FinancialAsOf { get; set; }

        public string ErrorMessage { get; set; } = "";
        public List<DailySales> SalesDays { get; set; } = new List<DailySales>();
        public DataTable LowStock { get; set; } = new DataTable();
        public DataTable NearExpiry { get; set; } = new DataTable();
        public DataTable Expired { get; set; } = new DataTable();

        public IndexModel(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void OnGet()
        {
            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                ErrorMessage = "The database connection is not configured.";
                return;
            }
            try
            {
                DataSet summary = Db.Query(connectionString, "dbo.usp_Dashboard_Summary");
                DataRow today = summary.Tables[0].Rows[0];
                TodaySales = Convert.ToDecimal(today["TotalAmount"]);
                TodayCount = Convert.ToInt32(today["SalesCount"]);

                DataSet alerts = Db.Query(connectionString, "dbo.usp_Dashboard_Alerts");
                LowStock = alerts.Tables[0];
                NearExpiry = alerts.Tables[1];
                Expired = alerts.Tables[2];
                LowStockCount = LowStock.Rows.Count;
                ExpiringCount = NearExpiry.Rows.Count;
                ExpiredCount = Expired.Rows.Count;

                if (User.IsInRole("Administrator"))
                {
                    DataSet financial = Db.Query(connectionString, "dbo.usp_Dashboard_FinancialSummary");
                    DataRow figures = financial.Tables[0].Rows[0];
                    MonthRevenue = Convert.ToDecimal(figures["SalesValue"]);
                    MonthStockCost = Convert.ToDecimal(figures["SoldStockCost"]);
                    MonthExpiryCost = Convert.ToDecimal(figures["EstimatedExpiryCost"]);
                    MissingCostItems = Convert.ToInt32(figures["MissingCostItems"]);
                    FinancialStart = Convert.ToDateTime(figures["PeriodStart"]);
                    FinancialAsOf = Convert.ToDateTime(figures["AsOfDate"]);
                }

                DataSet inventory = Db.Query(connectionString, "dbo.usp_Report_Inventory");
                ProductCount = inventory.Tables[0].Rows.Count;

                DateTime todayDate = DateTime.Today;
                DateTime start = todayDate.AddDays(-13);
                DataSet sales = Db.Query(connectionString, "dbo.usp_Report_Sales", Db.Param("@FromDate", start), Db.Param("@ToDate", todayDate));
                Dictionary<DateTime, decimal> totals = new Dictionary<DateTime, decimal>();
                foreach (DataRow row in sales.Tables[0].Rows)
                {
                    DateTime date = Convert.ToDateTime(row["SaleDay"]).Date;
                    totals[date] = Convert.ToDecimal(row["TotalAmount"]);
                }
                for (int i = 13; i >= 0; i--)
                {
                    DateTime date = todayDate.AddDays(-i);
                    decimal amount = totals.ContainsKey(date) ? totals[date] : 0;
                    if (i >= 7) PreviousWeekSales += amount;
                    else
                    {
                        WeekSales += amount;
                        SalesDays.Add(new DailySales { Date = date, Amount = amount });
                    }
                }
            }
            catch (SqlException)
            {
                ErrorMessage = "The dashboard could not load. Check the database connection and run the latest setup scripts.";
            }
        }

        public class DailySales
        {
            public DateTime Date { get; set; }
            public decimal Amount { get; set; }
        }
    }
}
