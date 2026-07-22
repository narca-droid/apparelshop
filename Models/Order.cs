using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApparelShop.Models;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}

public class Order
{
    public int Id { get; set; }

    [Required]
    public string OrderNumber { get; set; } = string.Empty;

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [Required, StringLength(150)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string CustomerPhone { get; set; } = string.Empty;

    [Required, StringLength(400)]
    public string ShippingAddress { get; set; } = string.Empty;

    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [StringLength(100)]
    public string Emirate { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal ShippingFee { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public string? Notes { get; set; }

    [StringLength(1000)]
    public string? ConfirmationNotes { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Required, StringLength(150)]
    public string ProductName { get; set; } = string.Empty;

    public string? Size { get; set; }
    public string? Color { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    [NotMapped]
    public decimal LineTotal => UnitPrice * Quantity;
}
