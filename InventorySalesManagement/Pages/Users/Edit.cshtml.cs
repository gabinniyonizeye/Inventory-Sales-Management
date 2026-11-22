using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InventorySalesManagement.Pages.Users
{
    public class EditModel : PageModel
    {
        private readonly IConfiguration configuration;

        public EditModel(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [BindProperty]
        public int UserId { get; set; }
        [BindProperty, Required, StringLength(150)]
        public string FullName { get; set; } = "";
        [BindProperty, Required]
        public string Role { get; set; } = "";
        [BindProperty]
        public bool IsActive { get; set; }
        public string UserName { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        public IActionResult OnGet(int id)
        {
            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("dbo.User_GetById", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add("@UserId", SqlDbType.Int).Value = id;
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return NotFound();
                            }
                            UserId = (int)reader["UserId"];
                            UserName = (string)reader["UserName"];
                            FullName = (string)reader["FullName"];
                            Role = (string)reader["Role"];
                            IsActive = (bool)reader["IsActive"];
                        }
                    }
                }
            }
            catch (SqlException)
            {
                ErrorMessage = "Unable to load this user.";
            }
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(FullName))
            {
                ErrorMessage = "Enter a name and select a valid role.";
                return Page();
            }
            if (Role != "Administrator" && Role != "Sales Staff")
            {
                ErrorMessage = "Enter a name and select a valid role.";
                return Page();
            }

            // An administrator cannot remove their own access by mistake.
            string? currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == UserId.ToString() && (!IsActive || Role != "Administrator"))
            {
                ErrorMessage = "You cannot deactivate your own account or remove your own administrator role.";
                return Page();
            }

            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("dbo.User_Update", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add("@UserId", SqlDbType.Int).Value = UserId;
                        command.Parameters.Add("@FullName", SqlDbType.NVarChar, 150).Value = FullName.Trim();
                        command.Parameters.Add("@Role", SqlDbType.NVarChar, 20).Value = Role;
                        command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = IsActive;
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException)
            {
                ErrorMessage = "Unable to update this user.";
                return Page();
            }
            return RedirectToPage("/Users/Index");
        }
    }
}
