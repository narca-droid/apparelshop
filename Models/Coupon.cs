using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApparelShop.Models;

public class Coupon
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    public CouponType Type { get; set; } = CouponType.Percentage;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Value { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? MinOrderAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? MaxDiscountAmount { get; set; }

    public int? UsageLimit { get; set; }
    public int TimesUsed { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt < DateTime.UtcNow;

    [NotMapped]
    public bool IsUsable => IsActive && !IsExpired && (UsageLimit == null || TimesUsed < UsageLimit);
}

public enum CouponType
{
    Percentage = 0,
    FixedAmount = 1
}
