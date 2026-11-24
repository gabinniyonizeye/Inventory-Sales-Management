using System.Data;
using InventorySalesManagement.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InventorySalesManagement.Pages.Receipts
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration configuration;
        public List<StockReceipt> Receipts { get; set; } = new List<StockReceipt>();
        public string ErrorMessage { get; set; } = string.Empty;
        public IndexModel(IConfiguration configuration) => this.configuration = configuration;

        public async Task OnGetAsync()
        {
            try
            {
                using SqlConnection connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
                using SqlCommand command = new SqlCommand("dbo.StockReceipt_GetAll", connection) { CommandType = CommandType.StoredProcedure };
                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Receipts.Add(new StockReceipt
                    {
                        StockReceiptId = reader.GetInt32(reader.GetOrdinal("StockReceiptId")),
                        SupplierId = reader.GetInt32(reader.GetOrdinal("SupplierId")),
                        SupplierName = reader.GetString(reader.GetOrdinal("SupplierName")),
                        ReceivedByUserId = reader.GetInt32(reader.GetOrdinal("ReceivedByUserId")),
                        ReceivedAt = reader.GetDateTime(reader.GetOrdinal("ReceivedAt")),
                        Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes"))
                    });
                }
            }
            catch (SqlException)
            {
                ErrorMessage = "Unable to load receipt history.";
            }
        }
    }
}
