using System.Data;
using InventorySalesManagement.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InventorySalesManagement.Pages.Sales;

[Authorize]
public class CreateModel : PageModel
{
    private readonly string _cs;
    public CreateModel(IConfiguration config) => _cs = Db.ConnectionString(config);

    public class ProductOption
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal SellingPrice { get; set; }
        public int Available { get; set; }
    }

    public class ItemInput
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public List<ProductOption> Products { get; private set; } = new();

    [BindProperty]
    public List<ItemInput> Items { get; set; } = new();

    public void OnGet() => LoadProducts();

    public IActionResult OnPost()
    {
        if (Items == null || Items.Count == 0)
        {
            ModelState.AddModelError("", "Add at least one product to the sale.");
        }
        else
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].ProductId <= 0)
                    ModelState.AddModelError("", $"Select a product for item {i + 1}.");
                if (Items[i].Quantity <= 0)
                    ModelState.AddModelError("", $"Enter a quantity greater than zero for item {i + 1}.");
            }
        }

        if (!ModelState.IsValid)
        {
            LoadProducts();
            return Page();
        }
        var items = Items!;

        var userId = Db.CurrentUserId(User, _cs);
        if (userId is null)
        {
            ModelState.AddModelError("", "Could not identify the logged-in user. Please sign in again.");
            LoadProducts();
            return Page();
        }

        var table = new DataTable();
        table.Columns.Add("ProductId", typeof(int));
        table.Columns.Add("Quantity", typeof(int));
        foreach (var i in items) table.Rows.Add(i.ProductId, i.Quantity);

        try
        {
            using var con = new SqlConnection(_cs);
            using var cmd = new SqlCommand("dbo.usp_CreateSale", con) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@SoldByUserId", userId.Value);
            var list = cmd.Parameters.AddWithValue("@Items", table);
            list.SqlDbType = SqlDbType.Structured;
            list.TypeName = "dbo.SaleItemList";
            var saleId = new SqlParameter("@SaleId", SqlDbType.Int) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(saleId);
            con.Open();
            cmd.ExecuteNonQuery();

            TempData["Success"] = $"Sale #{(int)saleId.Value} was recorded successfully.";
            return RedirectToPage("Details", new { id = (int)saleId.Value });
        }
        catch (SqlException ex) when (ex.Number == 50001)
        {
            ModelState.AddModelError("", ex.Message);
        }
        catch (SqlException)
        {
            ModelState.AddModelError("", "The sale could not be saved. Please try again.");
        }

        LoadProducts();
        return Page();
    }

    private void LoadProducts()
    {
        var ds = Db.Query(_cs, "dbo.Product_GetAll");
        foreach (DataRow r in ds.Tables[0].Rows)
        {
            if (!Convert.ToBoolean(r["IsActive"])) continue;
            var available = Convert.ToInt32(r["AvailableQuantity"]);
            if (available <= 0) continue;
            Products.Add(new ProductOption
            {
                ProductId = Convert.ToInt32(r["ProductId"]),
                ProductName = Convert.ToString(r["ProductName"]) ?? "",
                Unit = Convert.ToString(r["Unit"]) ?? "",
                SellingPrice = Convert.ToDecimal(r["SellingPrice"]),
                Available = available
            });
        }
    }
}
