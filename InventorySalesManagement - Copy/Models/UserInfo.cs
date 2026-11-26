namespace InventorySalesManagement.Models;

public class UserInfo
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string UserName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
