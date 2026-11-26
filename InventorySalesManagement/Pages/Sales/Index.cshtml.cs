using System.Data;
using InventorySalesManagement.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InventorySalesManagement.Pages.Sales;

[Authorize]
public class IndexModel : PageModel
{
    private readonly string _cs;
    public IndexModel(IConfiguration config) => _cs = Db.ConnectionString(config);

    [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }

    public DataTable Sales { get; private set; } = new();

    public void OnGet()
    {
        var ds = Db.Query(_cs, "dbo.usp_Sale_GetAll",
            Db.Param("@FromDate", From?.Date),
            Db.Param("@ToDate", To?.Date));
        Sales = ds.Tables[0];
    }
}
