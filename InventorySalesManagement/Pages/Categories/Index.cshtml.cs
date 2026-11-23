using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using InventorySalesManagement.Models;
using System.Data;

namespace InventorySalesManagement.Pages.Categories
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IList<Category> Categories { get; set; } = new List<Category>();

        public async Task OnGetAsync()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;
            Categories = new List<Category>();

            // WHY: We use a SQL Connection and DataReader to fetch rows from the Categories table
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.Category_GetAll", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    await conn.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Categories.Add(new Category
                            {
                                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                                CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                            });
                        }
                    }
                }
            }
        }

        // WHY: When you click "Deactivate", this method flips the boolean in the database
        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;
            Category? category = null;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.Category_GetAll", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    await conn.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (reader.GetInt32(reader.GetOrdinal("CategoryId")) == id)
                            {
                                category = new Category
                                {
                                    CategoryId = id,
                                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                                };
                                break;
                            }
                        }
                    }
                }
            }

            if (category != null)
            {
                category.IsActive = !category.IsActive; // Flip the status

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("dbo.Category_Save", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CategoryId", category.CategoryId);
                        cmd.Parameters.AddWithValue("@CategoryName", category.CategoryName);
                        cmd.Parameters.AddWithValue("@Description", (object?)category.Description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", category.IsActive);
                        await conn.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                TempData["SuccessMessage"] = $"Category '{category.CategoryName}' status updated.";
            }

            return RedirectToPage();
        }
    }
}