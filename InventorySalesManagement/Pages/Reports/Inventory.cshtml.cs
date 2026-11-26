using System.Data;
using InventorySalesManagement.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InventorySalesManagement.Pages.Reports;

[Authorize]
public class InventoryModel : PageModel
{
    private readonly string _cs;
    public InventoryModel(IConfiguration config) => _cs = Db.ConnectionString(config);

    public DataTable Stock { get; private set; } = new();

    public void OnGet()
    {
        Stock = Db.Query(_cs, "dbo.usp_Report_Inventory").Tables[0];
    }
}
