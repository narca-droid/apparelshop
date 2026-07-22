using System.ComponentModel.DataAnnotations;

namespace ApparelShop.Models;

public class StockNotification
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Required, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    public bool IsNotified { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? NotifiedAt { get; set; }
}
