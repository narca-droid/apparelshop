namespace ApparelShop.Models;

// Stored in session (not DB) for simplicity.
public class WishlistItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public string? ImageUrl { get; set; }
}

public class Wishlist
{
    public List<WishlistItem> Items { get; set; } = new();
    public int TotalItems => Items.Count;
}
