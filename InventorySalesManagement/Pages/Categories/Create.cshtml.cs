using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using InventorySalesManagement.Models;
using System.Data;

namespace InventorySalesManagement.Pages.Categories
{
    public class CreateModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public CreateModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // WHY: [BindProperty] connects the HTML form inputs to this C# object automatically
        [BindProperty]
        public Category Category { get; set; } = new Category();

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // WHY: If the user leaves the name blank, this stops the code and shows the error
            if (!ModelState.IsValid)
            {
                return Page();
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;

            // WHY: The assignment requires us to check for duplicate names.
            // We fetch all categories and compare the names.
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
                            string existingName = reader.GetString(reader.GetOrdinal("CategoryName"));
                            if (existingName.Equals(Category.CategoryName, StringComparison.OrdinalIgnoreCase))
                            {
                                // WHY: This adds a red error message to the screen
                                ModelState.AddModelError("Category.CategoryName", "A category with this name already exists.");
                                return Page();
                            }
                        }
                    }
                }
            }

            // WHY: If it's not a duplicate, we save it using the stored procedure
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.Category_Save", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CategoryName", Category.CategoryName.Trim());
                    // WHY: If Description is null, we pass DBNull.Value so SQL Server handles it correctly
                    cmd.Parameters.AddWithValue("@Description", (object?)Category.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", true); // New categories are active by default
                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            // WHY: TempData passes a message to the next page (Index) to show a green success alert
            TempData["SuccessMessage"] = "Category created successfully.";
            return RedirectToPage("./Index");
        }
    }
}