using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using InventorySalesManagement.Models;
using System.Data;

namespace InventorySalesManagement.Pages.Products
{
    [Authorize(Roles = "Administrator")]
    public class CreateModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public CreateModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // THIS IS THE MISSING LINE THAT CAUSED THE ERROR
        [BindProperty]
        public Product Product { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            await PopulateCategoriesDropdownAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await PopulateCategoriesDropdownAsync();
                return Page();
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;

            using SqlConnection conn = new(connectionString);
            using SqlCommand cmd = new("dbo.Product_Save", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            // NOTE: We DO NOT pass ProductId here because it is a NEW product.
            cmd.Parameters.AddWithValue("@CategoryId", Product.CategoryId);
            cmd.Parameters.AddWithValue("@ProductName", Product.ProductName.Trim());
            cmd.Parameters.AddWithValue("@SellingPrice", Product.SellingPrice);
            cmd.Parameters.AddWithValue("@ReorderLevel", Product.ReorderLevel);
            cmd.Parameters.AddWithValue("@Unit", Product.Unit.Trim());
            cmd.Parameters.AddWithValue("@IsActive", true);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            TempData["SuccessMessage"] = "Product created successfully.";
            return RedirectToPage("./Index");
        }

        private async Task PopulateCategoriesDropdownAsync()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;
            var categories = new List<Category>();

            using SqlConnection conn = new(connectionString);
            using SqlCommand cmd = new("SELECT CategoryId, CategoryName FROM dbo.Categories WHERE IsActive = 1", conn);
            await conn.OpenAsync();
            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new Category
                {
                    CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName"))
                });
            }

            ViewData["CategoryId"] = new SelectList(categories, "CategoryId", "CategoryName");
        }
    }
}