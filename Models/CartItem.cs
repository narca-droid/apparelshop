namespace ApparelShop.Models;

// Stored in session as JSON (Cart), not in the database.
public class CartItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public int Quantity { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}

public class Cart
{
    public List<CartItem> Items { get; set; } = new();

    public decimal Subtotal => Items.Sum(i => i.LineTotal);
    public int TotalQuantity => Items.Sum(i => i.Quantity);
}
