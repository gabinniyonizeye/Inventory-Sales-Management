using System.Data;
using InventorySalesManagement.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InventorySalesManagement.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration configuration;
        public List<UserInfo> Users = new List<UserInfo>();
        public string ErrorMessage = "";

        public IndexModel(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void OnGet()
        {
            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("dbo.User_GetAll", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                UserInfo user = new UserInfo();
                                user.UserId = (int)reader["UserId"];
                                user.FullName = (string)reader["FullName"];
                                user.UserName = (string)reader["UserName"];
                                user.Role = (string)reader["Role"];
                                user.IsActive = (bool)reader["IsActive"];
                                user.CreatedAt = (DateTime)reader["CreatedAt"];
                                Users.Add(user);
                            }
                        }
                    }
                }
            }
            catch (SqlException)
            {
                ErrorMessage = "Unable to load users. Please check the database connection.";
            }
        }
    }
}
