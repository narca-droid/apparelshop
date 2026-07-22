using System.ComponentModel.DataAnnotations;

namespace ApparelShop.Models;

public class Category
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public int? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
