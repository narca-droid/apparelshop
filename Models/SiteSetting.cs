using System.ComponentModel.DataAnnotations;

namespace ApparelShop.Models;

// Singleton row (Id = 1) that drives global site branding & layout.
public class SiteSetting
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string SiteName { get; set; } = "AURA Apparel";

    [StringLength(200)]
    public string Tagline { get; set; } = "Wear your story.";

    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }

    // Theme colors as hex, editable from admin
    [StringLength(20)]
    public string PrimaryColor { get; set; } = "#1a1a1a";

    [StringLength(20)]
    public string AccentColor { get; set; } = "#c9a227";

    [StringLength(20)]
    public string BackgroundColor { get; set; } = "#faf9f6";

    // Hero / homepage banner
    [StringLength(200)]
    public string HeroHeadline { get; set; } = "New Season. New Silhouettes.";

    [StringLength(400)]
    public string HeroSubtext { get; set; } = "Discover the Autumn/Winter collection, crafted for everyday elegance.";

    public string? HeroImageUrl { get; set; }
    public string? HeroButtonText { get; set; } = "Shop the Collection";
    public string? HeroButtonUrl { get; set; } = "/shop";

    // Footer / contact info
    [StringLength(200)]
    public string ContactEmail { get; set; } = "support@example.com";

    [StringLength(50)]
    public string ContactPhone { get; set; } = "+971 4 000 0000";

    [StringLength(300)]
    public string Address { get; set; } = "Sharjah, United Arab Emirates";

    public string? FacebookUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? TiktokUrl { get; set; }

    [StringLength(1000)]
    public string FooterAboutText { get; set; } = "Contemporary apparel designed with intention, made to last beyond a single season.";

    public bool ShowNewArrivalsSection { get; set; } = true;
    public bool ShowFeaturedSection { get; set; } = true;

    // Free-form SEO
    [StringLength(200)]
    public string MetaTitle { get; set; } = "AURA Apparel | Contemporary Fashion";

    [StringLength(400)]
    public string MetaDescription { get; set; } = "Shop contemporary apparel for men and women.";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// Flexible named content blocks admin can add/edit (e.g. "about-page", "shipping-policy", promo banners)
public class ContentBlock
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Key { get; set; } = string.Empty; // e.g. "about-page", "promo-banner-top"

    [Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;

    public string BodyHtml { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
