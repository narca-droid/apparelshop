using System.ComponentModel.DataAnnotations;

namespace ApparelShop.Models;

public class AuditLog
{
    public int Id { get; set; }

    [StringLength(100)]
    public string? UserId { get; set; }

    [StringLength(150)]
    public string? UserName { get; set; }

    [Required, StringLength(50)]
    public string Action { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Entity { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    [StringLength(2000)]
    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
