using System.ComponentModel.DataAnnotations;

namespace InventorySalesManagement.Models
{
    public class Supplier
    {
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "Supplier name is required.")]
        [StringLength(150, ErrorMessage = "Supplier name cannot exceed 150 characters.")]
        public string SupplierName { get; set; } = string.Empty;

        [StringLength(30, ErrorMessage = "Phone cannot exceed 30 characters.")]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string? Email { get; set; }

        [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters.")]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}
