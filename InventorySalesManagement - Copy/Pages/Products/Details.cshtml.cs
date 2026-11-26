using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using InventorySalesManagement.Models;
using System.Data;

namespace InventorySalesManagement.Pages.Products
{
    public class DetailsModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public DetailsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Product Product { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;

            using SqlConnection conn = new(connectionString);
            using SqlCommand cmd = new("dbo.Product_GetAll", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            await conn.OpenAsync();

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (reader.GetInt32(reader.GetOrdinal("ProductId")) == id)
                {
                    Product = new Product
                    {
                        ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                        ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                        CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                        SellingPrice = reader.GetDecimal(reader.GetOrdinal("SellingPrice")),
                        ReorderLevel = reader.GetInt32(reader.GetOrdinal("ReorderLevel")),
                        Unit = reader.GetString(reader.GetOrdinal("Unit")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        AvailableQuantity = reader.GetInt32(reader.GetOrdinal("AvailableQuantity")) // Fetches the calculated stock
                    };
                    break;
                }
            }

            if (Product.ProductId == 0) return NotFound();

            return Page();
        }
    }
}