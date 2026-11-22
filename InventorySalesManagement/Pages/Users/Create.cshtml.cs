using System.ComponentModel.DataAnnotations;
using System.Data;
using InventorySalesManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InventorySalesManagement.Pages.Users
{
    public class CreateModel : PageModel
    {
        private readonly IConfiguration configuration;

        public CreateModel(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [BindProperty, Required, StringLength(150)]
        public string FullName { get; set; } = "";
        [BindProperty, Required, StringLength(100)]
        public string UserName { get; set; } = "";
        [BindProperty, Required, StringLength(100, MinimumLength = 10), DataType(DataType.Password)]
        public string Password { get; set; } = "";
        [BindProperty, Required]
        public string Role { get; set; } = "Sales Staff";
        public string ErrorMessage = "";

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(UserName))
            {
                ErrorMessage = "Please complete all fields. Passwords need at least 10 characters.";
                return Page();
            }
            if (Role != "Administrator" && Role != "Sales Staff")
            {
                ErrorMessage = "Choose a valid role.";
                return Page();
            }

            UserInfo user = new UserInfo();
            user.FullName = FullName.Trim();
            user.UserName = UserName.Trim();
            user.Role = Role;

            // Store a password hash, never the password itself.
            PasswordHasher<UserInfo> hasher = new PasswordHasher<UserInfo>();
            string passwordHash = hasher.HashPassword(user, Password);
            string? connectionString = configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("dbo.User_Create", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add("@FullName", SqlDbType.NVarChar, 150).Value = user.FullName;
                        command.Parameters.Add("@UserName", SqlDbType.NVarChar, 100).Value = user.UserName;
                        command.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 500).Value = passwordHash;
                        command.Parameters.Add("@Role", SqlDbType.NVarChar, 20).Value = user.Role;
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601 || ex.Number == 2627)
                {
                    ErrorMessage = "That username is already in use.";
                }
                else
                {
                    ErrorMessage = "Unable to save the user. Please check the database connection.";
                }
                return Page();
            }
            return RedirectToPage("/Users/Index");
        }
    }
}
