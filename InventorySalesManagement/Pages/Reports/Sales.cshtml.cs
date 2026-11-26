using System.Data;
using InventorySalesManagement.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InventorySalesManagement.Pages.Reports;

[Authorize]
public class SalesModel : PageModel
{
    private readonly string _cs;
    public SalesModel(IConfiguration config) => _cs = Db.ConnectionString(config);

    [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }

    public DataTable ByDay { get; private set; } = new();
    public DataTable ByProduct { get; private set; } = new();
    public decimal GrandTotal { get; private set; }

    public void OnGet() => Load();

    /// <summary>Download as CSV. part = "days" or "products".</summary>
    public IActionResult OnGetDownload(string? part)
    {
        Load();
        var table = part == "products" ? ByProduct : ByDay;
        var name = $"sales-report-{(part == "products" ? "products" : "days")}-{From:yyyyMMdd}-{To:yyyyMMdd}.csv";
        return CsvExport.ToFile(table, name);
    }

    private void Load()
    {
        To ??= DateTime.Today;
        From ??= To.Value.AddDays(-30);

        var ds = Db.Query(_cs, "dbo.usp_Report_Sales",
            Db.Param("@FromDate", From.Value.Date),
            Db.Param("@ToDate", To.Value.Date));
        ByDay = ds.Tables[0];
        ByProduct = ds.Tables[1];

        GrandTotal = 0;
        foreach (DataRow r in ByDay.Rows) GrandTotal += (decimal)r["TotalAmount"];
    }
}