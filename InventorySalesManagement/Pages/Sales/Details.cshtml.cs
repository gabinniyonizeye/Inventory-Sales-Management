using System.Data;
using InventorySalesManagement.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InventorySalesManagement.Pages.Sales;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly string _cs;
    public DetailsModel(IConfiguration config) => _cs = Db.ConnectionString(config);

    public DataRow? Header { get; private set; }
    public DataTable Lines { get; private set; } = new();

    public IActionResult OnGet(int id)
    {
        var ds = Db.Query(_cs, "dbo.usp_Sale_GetDetails", Db.Param("@SaleId", id));
        if (ds.Tables.Count < 2 || ds.Tables[0].Rows.Count == 0) return NotFound();
        Header = ds.Tables[0].Rows[0];
        Lines = ds.Tables[1];
        return Page();
    }
}
