using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using InventorySalesManagement.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InventorySalesManagement.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration configuration;

        public LoginModel(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [BindProperty]
        [Required]
        [StringLength(100)]
        public string UserName { get; set; } = "";

        [BindProperty]
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public string ErrorMessage { get; set; } = "";

        public async Task<IActionResult> OnPostAsync(string? returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                ErrorMessage = "Database connection is not configured.";
                return Page();
            }

            UserInfo? user = null;
            try
            {
                // Find the account using the username entered in the form.
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("dbo.User_GetByUsername", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add("@UserName", SqlDbType.NVarChar, 100).Value = UserName.Trim();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                user = new UserInfo();
                                user.UserId = (int)reader["UserId"];
                                user.FullName = (string)reader["FullName"];
                                user.UserName = (string)reader["UserName"];
                                user.PasswordHash = (string)reader["PasswordHash"];
                                user.Role = (string)reader["Role"];
                                user.IsActive = (bool)reader["IsActive"];
                            }
                        }
                    }
                }
            }
            catch (SqlException)
            {
                ErrorMessage = "Unable to connect to the database. Please try again.";
                return Page();
            }

            if (user == null || !user.IsActive)
            {
                ErrorMessage = "Invalid username or password.";
                return Page();
            }

            // Compare the entered password with the secure hash stored in the database.
            PasswordHasher<UserInfo> hasher = new PasswordHasher<UserInfo>();
            PasswordVerificationResult result = hasher.VerifyHashedPassword(user, user.PasswordHash, Password);
            if (result == PasswordVerificationResult.Failed)
            {
                ErrorMessage = "Invalid username or password.";
                return Page();
            }

            // Save the user ID, name and role in the login session.
            List<Claim> claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()));
            claims.Add(new Claim(ClaimTypes.Name, user.FullName));
            claims.Add(new Claim(ClaimTypes.Role, user.Role));
            ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToPage("/Index");
        }
    }
}
