using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using InventorySalesManagement.Models;
using System.Data;

namespace InventorySalesManagement.Pages.Categories
{
    public class EditModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public EditModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public Category Category { get; set; } = new Category();

        // WHY: This runs when the page loads. It fetches the existing category details using the ID from the URL.
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                // WHY: We use a direct SELECT query here to get just the one category we want to edit.
                using (SqlCommand cmd = new SqlCommand("SELECT CategoryId, CategoryName, Description, IsActive FROM dbo.Categories WHERE CategoryId = @CategoryId", conn))
                {
                    cmd.Parameters.AddWithValue("@CategoryId", id);
                    await conn.OpenAsync();
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            Category = new Category
                            {
                                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                                CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                            };
                        }
                        else
                        {
                            return NotFound(); // WHY: If the ID doesn't exist in the database, show a 404 error.
                        }
                    }
                }
            }

            return Page();
        }

        // WHY: This runs when the user clicks "Save". It updates the database.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;

            // WHY: We check for duplicate names, but we MUST ignore the current category's ID so it doesn't flag itself as a duplicate.
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
                            int existingId = reader.GetInt32(reader.GetOrdinal("CategoryId"));
                            string existingName = reader.GetString(reader.GetOrdinal("CategoryName"));

                            // Check if name matches AND it's a different ID
                            if (existingId != Category.CategoryId && existingName.Equals(Category.CategoryName, StringComparison.OrdinalIgnoreCase))
                            {
                                ModelState.AddModelError("Category.CategoryName", "A different category with this name already exists.");
                                return Page();
                            }
                        }
                    }
                }
            }

            // WHY: Now we save the updated information using the stored procedure.
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.Category_Save", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CategoryId", Category.CategoryId); // Crucial for updating the correct row
                    cmd.Parameters.AddWithValue("@CategoryName", Category.CategoryName.Trim());
                    cmd.Parameters.AddWithValue("@Description", (object?)Category.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", Category.IsActive);
                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            TempData["SuccessMessage"] = "Category updated successfully.";
            return RedirectToPage("./Index");
        }
    }
}