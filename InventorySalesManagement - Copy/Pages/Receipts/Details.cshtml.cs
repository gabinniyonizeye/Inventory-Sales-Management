using System.Data;
using InventorySalesManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InventorySalesManagement.Pages.Receipts
{
    public class DetailsModel : PageModel
    {
        private readonly IConfiguration configuration;
        public StockReceipt Receipt { get; set; } = new StockReceipt();
        public string ErrorMessage { get; set; } = string.Empty;
        public DetailsModel(IConfiguration configuration) => this.configuration = configuration;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                using SqlConnection connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
                using SqlCommand command = new SqlCommand("dbo.StockReceipt_GetById", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.Add("@StockReceiptId", SqlDbType.Int).Value = id;
                await connection.OpenAsync();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return NotFound();
                Receipt = new StockReceipt
                {
                    StockReceiptId = reader.GetInt32(reader.GetOrdinal("StockReceiptId")),
                    SupplierId = reader.GetInt32(reader.GetOrdinal("SupplierId")),
                    SupplierName = reader.GetString(reader.GetOrdinal("SupplierName")),
                    ReceivedByUserId = reader.GetInt32(reader.GetOrdinal("ReceivedByUserId")),
                    ReceivedByName = reader.GetString(reader.GetOrdinal("ReceivedByName")),
                    ReceivedAt = reader.GetDateTime(reader.GetOrdinal("ReceivedAt")),
                    Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes"))
                };
                await reader.NextResultAsync();
                while (await reader.ReadAsync())
                {
                    Receipt.Batches.Add(new StockBatch
                    {
                        StockBatchId = reader.GetInt32(reader.GetOrdinal("StockBatchId")),
                        StockReceiptId = Receipt.StockReceiptId,
                        ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                        ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                        QuantityReceived = reader.GetInt32(reader.GetOrdinal("QuantityReceived")),
                        QuantityRemaining = reader.GetInt32(reader.GetOrdinal("QuantityRemaining")),
                        UnitCost = reader.GetDecimal(reader.GetOrdinal("UnitCost")),
                        ExpiryDate = reader.IsDBNull(reader.GetOrdinal("ExpiryDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ExpiryDate")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                    });
                }
                return Page();
            }
            catch (SqlException)
            {
                ErrorMessage = "Unable to load receipt details.";
                return Page();
            }
        }
    }
}
