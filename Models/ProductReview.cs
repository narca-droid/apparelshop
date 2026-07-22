using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApparelShop.Models;

public class ProductReview
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Required, StringLength(150)]
    public string ReviewerName { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string ReviewerEmail { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; set; }

    [Required, StringLength(500)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Body { get; set; }

    public bool IsApproved { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
