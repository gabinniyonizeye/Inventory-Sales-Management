using System.Data;
using System.Security.Claims;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using InventorySalesManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InventorySalesManagement.Pages.Receipts
{
    public class CreateModel : PageModel
    {
        private readonly IConfiguration configuration;
        public List<Supplier> Suppliers { get; set; } = new List<Supplier>();
        public List<Product> Products { get; set; } = new List<Product>();
        [BindProperty, Range(1, int.MaxValue, ErrorMessage = "Select a supplier.")] public int SupplierId { get; set; }
        [BindProperty] public string? Notes { get; set; }
        [BindProperty] public List<StockReceiptLine> Lines { get; set; } = new List<StockReceiptLine> { new StockReceiptLine() };
        public string ErrorMessage { get; set; } = string.Empty;

        public CreateModel(IConfiguration configuration) => this.configuration = configuration;

        public async Task OnGetAsync()
        {
            await LoadOptionsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Lines == null || Lines.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Add at least one product to the receipt.");
            }
            else
            {
                if (Lines.GroupBy(line => line.ProductId).Any(group => group.Key <= 0 || group.Count() > 1))
                    ModelState.AddModelError(string.Empty, "Select each product only once per receipt.");
                for (int index = 0; index < Lines.Count; index++)
                {
                    if (Lines[index].Quantity <= 0)
                        ModelState.AddModelError($"Lines[{index}].Quantity", "Quantity must be greater than zero.");
                    if (Lines[index].UnitCost < 0)
                        ModelState.AddModelError($"Lines[{index}].UnitCost", "Purchase cost cannot be negative.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadOptionsAsync();
                return Page();
            }

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                ErrorMessage = "Your login session is invalid. Sign in again.";
                await LoadOptionsAsync();
                return Page();
            }

            try
            {
                using SqlConnection connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
                using SqlCommand command = new SqlCommand("dbo.StockReceipt_Create", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.Add("@SupplierId", SqlDbType.Int).Value = SupplierId;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@Items", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(Lines!.Select(line => new
                {
                    productId = line.ProductId,
                    quantity = line.Quantity,
                    unitCost = line.UnitCost,
                    expiryDate = line.ExpiryDate?.ToString("yyyy-MM-dd")
                }));
                command.Parameters.Add("@Notes", SqlDbType.NVarChar, 250).Value = string.IsNullOrWhiteSpace(Notes) ? DBNull.Value : Notes.Trim();
                await connection.OpenAsync();
                object? value = await command.ExecuteScalarAsync();
                int receiptId = Convert.ToInt32(value);
                TempData["SuccessMessage"] = $"Receipt #{receiptId} was saved successfully with {Lines!.Count} batch(es).";
                return RedirectToPage("Index");
            }
            catch (SqlException ex)
            {
                ErrorMessage = ex.Number >= 50000 ? ex.Message : "Unable to save the receipt. No changes were committed.";
                await LoadOptionsAsync();
                return Page();
            }
        }

        private async Task LoadOptionsAsync()
        {
            Suppliers = new List<Supplier>();
            Products = new List<Product>();
            using SqlConnection connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();
            using (SqlCommand supplierCommand = new SqlCommand("dbo.Supplier_GetAll", connection))
            {
                supplierCommand.CommandType = CommandType.StoredProcedure;
                supplierCommand.Parameters.Add("@SearchTerm", SqlDbType.NVarChar, 150).Value = DBNull.Value;
                using SqlDataReader reader = await supplierCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (reader.GetBoolean(reader.GetOrdinal("IsActive")))
                        Suppliers.Add(new Supplier { SupplierId = reader.GetInt32(reader.GetOrdinal("SupplierId")), SupplierName = reader.GetString(reader.GetOrdinal("SupplierName")) });
                }
            }
            using (SqlCommand productCommand = new SqlCommand("dbo.Product_GetAll", connection))
            {
                productCommand.CommandType = CommandType.StoredProcedure;
                productCommand.Parameters.Add("@SearchTerm", SqlDbType.NVarChar, 150).Value = DBNull.Value;
                productCommand.Parameters.Add("@CategoryId", SqlDbType.Int).Value = DBNull.Value;
                using SqlDataReader reader = await productCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (reader.GetBoolean(reader.GetOrdinal("IsActive")))
                        Products.Add(new Product { ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")), ProductName = reader.GetString(reader.GetOrdinal("ProductName")), Unit = reader.GetString(reader.GetOrdinal("Unit")) });
                }
            }
        }
    }
}
