using System.ComponentModel.DataAnnotations;

namespace InventorySalesManagement.Models
{
    public class StockReceipt
    {
        public int StockReceiptId { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int ReceivedByUserId { get; set; }
        public DateTime ReceivedAt { get; set; }

        [StringLength(250, ErrorMessage = "Notes cannot exceed 250 characters.")]
        public string? Notes { get; set; }

        public List<StockBatch> Batches { get; set; } = new List<StockBatch>();
    }

    public class StockReceiptLine
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select a product.")]
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Purchase cost cannot be negative.")]
        public decimal UnitCost { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }
    }

    public class StockBatch
    {
        public int StockBatchId { get; set; }
        public int StockReceiptId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public int QuantityReceived { get; set; }
        public int QuantityRemaining { get; set; }
        public decimal UnitCost { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
