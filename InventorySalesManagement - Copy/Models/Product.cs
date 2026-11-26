using System.ComponentModel.DataAnnotations;

namespace InventorySalesManagement.Models
{
    public class Product
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(150)]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selling price is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
        public decimal SellingPrice { get; set; }

        [Required(ErrorMessage = "Unit is required (e.g., kg, pcs).")]
        [StringLength(30)]
        public string Unit { get; set; } = string.Empty;

        [Required(ErrorMessage = "Reorder level is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Reorder level cannot be negative.")]
        public int ReorderLevel { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        // These two are for display only (like available stock)
        public string CategoryName { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
    }
}