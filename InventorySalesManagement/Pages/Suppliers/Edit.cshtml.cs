using System.Data;
using InventorySalesManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InventorySalesManagement.Pages.Suppliers
{
    public class EditModel : PageModel
    {
        private readonly IConfiguration configuration;
        [BindProperty] public Supplier Supplier { get; set; } = new Supplier();
        public string ErrorMessage { get; set; } = string.Empty;
        public EditModel(IConfiguration configuration) => this.configuration = configuration;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                using SqlConnection connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
                using SqlCommand command = new SqlCommand("dbo.Supplier_GetById", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.Add("@SupplierId", SqlDbType.Int).Value = id;
                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return NotFound();
                Supplier = ReadSupplier(reader);
                return Page();
            }
            catch (SqlException)
            {
                ErrorMessage = "Unable to load the supplier.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Supplier.SupplierName = Supplier.SupplierName.Trim();
            Supplier.Phone = Clean(Supplier.Phone);
            Supplier.Email = Clean(Supplier.Email);
            Supplier.Address = Clean(Supplier.Address);
            if (!ModelState.IsValid) return Page();
            try
            {
                using SqlConnection connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
                using SqlCommand command = new SqlCommand("dbo.Supplier_Save", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.Add("@SupplierId", SqlDbType.Int).Value = Supplier.SupplierId;
                command.Parameters.Add("@SupplierName", SqlDbType.NVarChar, 150).Value = Supplier.SupplierName;
                command.Parameters.Add("@Phone", SqlDbType.NVarChar, 30).Value = (object?)Supplier.Phone ?? DBNull.Value;
                command.Parameters.Add("@Email", SqlDbType.NVarChar, 150).Value = (object?)Supplier.Email ?? DBNull.Value;
                command.Parameters.Add("@Address", SqlDbType.NVarChar, 250).Value = (object?)Supplier.Address ?? DBNull.Value;
                command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = Supplier.IsActive;
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                TempData["SuccessMessage"] = "Supplier updated successfully.";
                return RedirectToPage("Index");
            }
            catch (SqlException ex)
            {
                ErrorMessage = ex.Number >= 50000 ? ex.Message : "Unable to save the supplier.";
                return Page();
            }
        }

        private static Supplier ReadSupplier(SqlDataReader reader) => new Supplier
        {
            SupplierId = reader.GetInt32(reader.GetOrdinal("SupplierId")),
            SupplierName = reader.GetString(reader.GetOrdinal("SupplierName")),
            Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
            Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
            Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
