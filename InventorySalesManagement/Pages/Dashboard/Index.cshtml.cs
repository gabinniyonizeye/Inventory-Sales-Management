using InventorySalesManagement.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InventorySalesManagement.Pages.Dashboard;

[Authorize]
public class IndexModel : PageModel
{
    private readonly string _cs;
    public IndexModel(IConfiguration config) => _cs = Db.ConnectionString(config);

    public void OnGet() { }

    /// <summary>Called by the page when it opens and every 30 seconds afterwards.</summary>
    public IActionResult OnGetData()
    {
        var summary = Db.Query(_cs, "dbo.usp_Dashboard_Summary");
        var alerts = Db.Query(_cs, "dbo.usp_Dashboard_Alerts");

        return new JsonResult(new
        {
            summary = Db.ToRows(summary.Tables[0]).FirstOrDefault(),
            lowStock = Db.ToRows(alerts.Tables[0]),
            nearExpiry = Db.ToRows(alerts.Tables[1]),
            expired = Db.ToRows(alerts.Tables[2])
        });
    }
}
