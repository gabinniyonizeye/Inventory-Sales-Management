using System.Data;
using InventorySalesManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InventorySalesManagement.Pages.Suppliers
{
    public class CreateModel : PageModel
    {
        private readonly IConfiguration configuration;
        [BindProperty] public Supplier Supplier { get; set; } = new Supplier();
        public string ErrorMessage { get; set; } = string.Empty;
        public CreateModel(IConfiguration configuration) => this.configuration = configuration;

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
                using SqlCommand command = BuildSaveCommand(connection);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                TempData["SuccessMessage"] = "Supplier added successfully.";
                return RedirectToPage("Index");
            }
            catch (SqlException ex)
            {
                ErrorMessage = ex.Number >= 50000 ? ex.Message : "Unable to save the supplier.";
                return Page();
            }
        }

        private SqlCommand BuildSaveCommand(SqlConnection connection)
        {
            SqlCommand command = new SqlCommand("dbo.Supplier_Save", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.Add("@SupplierName", SqlDbType.NVarChar, 150).Value = Supplier.SupplierName;
            command.Parameters.Add("@Phone", SqlDbType.NVarChar, 30).Value = (object?)Supplier.Phone ?? DBNull.Value;
            command.Parameters.Add("@Email", SqlDbType.NVarChar, 150).Value = (object?)Supplier.Email ?? DBNull.Value;
            command.Parameters.Add("@Address", SqlDbType.NVarChar, 250).Value = (object?)Supplier.Address ?? DBNull.Value;
            command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = Supplier.IsActive;
            return command;
        }

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
