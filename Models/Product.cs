using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApparelShop.Models;

public class Product
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(300)]
    public string? ShortDescription { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Range(0, 999999)]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? CompareAtPrice { get; set; }

    [Required, StringLength(50)]
    public string Sku { get; set; } = string.Empty;

    public int StockQuantity { get; set; } = 0;

    // Comma-separated sizes/colors for simplicity, e.g. "S,M,L,XL"
    [StringLength(200)]
    public string? Sizes { get; set; }

    [StringLength(200)]
    public string? Colors { get; set; }

    [StringLength(300)]
    public string? Material { get; set; }

    public bool IsFeatured { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public bool IsNewArrival { get; set; } = false;

    public bool IsDeleted { get; set; } = false;

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public string? MainImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [NotMapped]
    public bool OnSale => CompareAtPrice.HasValue && CompareAtPrice > Price;

    [NotMapped]
    public int DiscountPercent => OnSale
        ? (int)Math.Round(100 - (Price / CompareAtPrice!.Value * 100))
        : 0;
}
