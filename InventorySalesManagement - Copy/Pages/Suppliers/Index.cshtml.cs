using Microsoft.AspNetCore.Authorization;
using System.Data;
using InventorySalesManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InventorySalesManagement.Pages.Suppliers
{
    [Authorize(Roles = "Administrator")]
    public class IndexModel : PageModel
    {
        private readonly IConfiguration configuration;
        public List<Supplier> Suppliers { get; set; } = new List<Supplier>();
        public string SearchTerm { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public IndexModel(IConfiguration configuration) => this.configuration = configuration;

        public async Task OnGetAsync(string? searchTerm)
        {
            SearchTerm = searchTerm?.Trim() ?? string.Empty;
            try
            {
                using SqlConnection connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
                using SqlCommand command = new SqlCommand("dbo.Supplier_GetAll", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@SearchTerm", SqlDbType.NVarChar, 150).Value = (object)SearchTerm ?? DBNull.Value;
                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Suppliers.Add(new Supplier
                    {
                        SupplierId = reader.GetInt32(reader.GetOrdinal("SupplierId")),
                        SupplierName = reader.GetString(reader.GetOrdinal("SupplierName")),
                        Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                        Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                    });
                }
            }
            catch (SqlException)
            {
                ErrorMessage = "Unable to load suppliers. Please check the database connection and procedures.";
            }
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            try
            {
                using SqlConnection connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync();
                Supplier? supplier = null;
                using (SqlCommand get = new SqlCommand("dbo.Supplier_GetById", connection))
                {
                    get.CommandType = CommandType.StoredProcedure;
                    get.Parameters.Add("@SupplierId", SqlDbType.Int).Value = id;
                    using SqlDataReader reader = await get.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        supplier = new Supplier
                        {
                            SupplierId = id,
                            SupplierName = reader.GetString(reader.GetOrdinal("SupplierName")),
                            Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
                            Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                            Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                        };
                    }
                }

                if (supplier == null)
                {
                    TempData["ErrorMessage"] = "Supplier was not found.";
                    return RedirectToPage();
                }

                using SqlCommand save = new SqlCommand("dbo.Supplier_Save", connection);
                save.CommandType = CommandType.StoredProcedure;
                save.Parameters.Add("@SupplierId", SqlDbType.Int).Value = supplier.SupplierId;
                save.Parameters.Add("@SupplierName", SqlDbType.NVarChar, 150).Value = supplier.SupplierName;
                save.Parameters.Add("@Phone", SqlDbType.NVarChar, 30).Value = (object?)supplier.Phone ?? DBNull.Value;
                save.Parameters.Add("@Email", SqlDbType.NVarChar, 150).Value = (object?)supplier.Email ?? DBNull.Value;
                save.Parameters.Add("@Address", SqlDbType.NVarChar, 250).Value = (object?)supplier.Address ?? DBNull.Value;
                save.Parameters.Add("@IsActive", SqlDbType.Bit).Value = !supplier.IsActive;
                await save.ExecuteNonQueryAsync();
                TempData["SuccessMessage"] = supplier.IsActive ? "Supplier deactivated." : "Supplier activated.";
            }
            catch (SqlException)
            {
                TempData["ErrorMessage"] = "Unable to change the supplier status.";
            }
            return RedirectToPage();
        }
    }
}
