using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using InventorySalesManagement.Models;
using System.Data;

namespace InventorySalesManagement.Pages.Products
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IList<Product> Products { get; set; } = new List<Product>();
        public SelectList CategoryList { get; set; } = default!;

        // WHY: These properties hold the user's search text and chosen category filter
        public string SearchTerm { get; set; } = string.Empty;
        public int? CategoryFilter { get; set; }

        public async Task OnGetAsync(string? searchTerm, int? categoryFilter)
        {
            SearchTerm = searchTerm ?? string.Empty;
            CategoryFilter = categoryFilter;

            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;

            // WHY: First, we load the Categories into a dropdown so the Admin can filter products.
            var categories = new List<Category>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT CategoryId, CategoryName FROM dbo.Categories WHERE IsActive = 1", conn))
                {
                    await conn.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            categories.Add(new Category
                            {
                                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                                CategoryName = reader.GetString(reader.GetOrdinal("CategoryName"))
                            });
                        }
                    }
                }
            }
            CategoryList = new SelectList(categories, "CategoryId", "CategoryName");

            // WHY: Now we load the Products using the stored procedure we created earlier.
            // We pass the search term and category filter. If they are empty, it returns all products.
            Products = new List<Product>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.Product_GetAll", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@SearchTerm", (object?)searchTerm ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CategoryId", (object?)categoryFilter ?? DBNull.Value);

                    await conn.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Products.Add(new Product
                            {
                                ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                                ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                                CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                                SellingPrice = reader.GetDecimal(reader.GetOrdinal("SellingPrice")),
                                ReorderLevel = reader.GetInt32(reader.GetOrdinal("ReorderLevel")),
                                Unit = reader.GetString(reader.GetOrdinal("Unit")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                AvailableQuantity = reader.GetInt32(reader.GetOrdinal("AvailableQuantity"))
                            });
                        }
                    }
                }
            }
        }

        // WHY: This handles the Activate/Deactivate button. We don't delete products to preserve historical records.
        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;
            Product? product = null;

            // First, find the product so we can flip its IsActive status
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.Product_GetAll", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    await conn.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (reader.GetInt32(reader.GetOrdinal("ProductId")) == id)
                            {
                                product = new Product
                                {
                                    ProductId = id,
                                    ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                                    CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                                    SellingPrice = reader.GetDecimal(reader.GetOrdinal("SellingPrice")),
                                    ReorderLevel = reader.GetInt32(reader.GetOrdinal("ReorderLevel")),
                                    Unit = reader.GetString(reader.GetOrdinal("Unit")),
                                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                                };
                                break;
                            }
                        }
                    }
                }
            }

            if (product != null)
            {
                product.IsActive = !product.IsActive; // Flip status

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("dbo.Product_Save", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ProductId", product.ProductId);
                        cmd.Parameters.AddWithValue("@CategoryId", product.CategoryId);
                        cmd.Parameters.AddWithValue("@ProductName", product.ProductName);
                        cmd.Parameters.AddWithValue("@SellingPrice", product.SellingPrice);
                        cmd.Parameters.AddWithValue("@ReorderLevel", product.ReorderLevel);
                        cmd.Parameters.AddWithValue("@Unit", product.Unit);
                        cmd.Parameters.AddWithValue("@IsActive", product.IsActive);
                        await conn.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                TempData["SuccessMessage"] = $"Product '{product.ProductName}' status updated.";
            }

            return RedirectToPage();
        }
    }
}