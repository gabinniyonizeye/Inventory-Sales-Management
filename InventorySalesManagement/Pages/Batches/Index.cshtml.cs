using System.Data;
using InventorySalesManagement.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InventorySalesManagement.Pages.Batches
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration configuration;
        public List<StockBatch> Batches { get; set; } = new List<StockBatch>();
        public string ErrorMessage { get; set; } = string.Empty;
        public IndexModel(IConfiguration configuration) => this.configuration = configuration;

        public async Task OnGetAsync()
        {
            try
            {
                using SqlConnection connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
                using SqlCommand command = new SqlCommand("dbo.StockBatch_GetAll", connection) { CommandType = CommandType.StoredProcedure };
                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Batches.Add(new StockBatch
                    {
                        StockBatchId = reader.GetInt32(reader.GetOrdinal("StockBatchId")),
                        StockReceiptId = reader.GetInt32(reader.GetOrdinal("StockReceiptId")),
                        ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                        ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                        SupplierName = reader.GetString(reader.GetOrdinal("SupplierName")),
                        QuantityReceived = reader.GetInt32(reader.GetOrdinal("QuantityReceived")),
                        QuantityRemaining = reader.GetInt32(reader.GetOrdinal("QuantityRemaining")),
                        UnitCost = reader.GetDecimal(reader.GetOrdinal("UnitCost")),
                        ExpiryDate = reader.IsDBNull(reader.GetOrdinal("ExpiryDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ExpiryDate")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                    });
                }
            }
            catch (SqlException)
            {
                ErrorMessage = "Unable to load stock batches.";
            }
        }
    }
}
