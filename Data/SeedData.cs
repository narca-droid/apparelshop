using ApparelShop.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ApparelShop.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // Using EnsureCreated for a zero-setup first run (no pre-generated migrations).
        // For production, switch to migrations: `dotnet ef migrations add Init` then `context.Database.MigrateAsync()`.
        await context.Database.EnsureCreatedAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Roles
        foreach (var role in new[] { "Admin", "Customer" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Default admin account
        const string adminEmail = "admin@aura-apparel.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Store Administrator",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@Secure123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        // Site settings singleton
        if (!await context.SiteSettings.AnyAsync())
        {
            context.SiteSettings.Add(new SiteSetting());
            await context.SaveChangesAsync();
        }

        // Content blocks
        if (!await context.ContentBlocks.AnyAsync())
        {
            context.ContentBlocks.AddRange(
                new ContentBlock
                {
                    Key = "about-page",
                    Title = "About Us",
                    BodyHtml = "<p>AURA Apparel is a contemporary fashion label based in the UAE, designing considered pieces for everyday life. Every garment is chosen for quality, fit, and longevity.</p>"
                },
                new ContentBlock
                {
                    Key = "shipping-policy",
                    Title = "Shipping & Returns",
                    BodyHtml = "<p>We ship across the UAE within 2-4 business days. Returns are accepted within 14 days of delivery, provided items are unworn and tagged.</p>"
                },
                new ContentBlock
                {
                    Key = "promo-banner-top",
                    Title = "Top Announcement Bar",
                    BodyHtml = "Free shipping across the UAE on orders over AED 300"
                }
            );
            await context.SaveChangesAsync();
        }

        // Categories + products
        if (!await context.Categories.AnyAsync())
        {
            var women = new Category { Name = "Women", Slug = "women", DisplayOrder = 1, Description = "Womenswear essentials and statement pieces." };
            var men = new Category { Name = "Men", Slug = "men", DisplayOrder = 2, Description = "Modern menswear staples." };
            var accessories = new Category { Name = "Accessories", Slug = "accessories", DisplayOrder = 3, Description = "Finishing touches." };
            var footwear = new Category { Name = "Footwear", Slug = "footwear", DisplayOrder = 4, Description = "Shoes for every occasion." };

            context.Categories.AddRange(women, men, accessories, footwear);
            await context.SaveChangesAsync();

            var products = new List<Product>
            {
                new()
                {
                    Name = "Tailored Wool Blazer",
                    Slug = "tailored-wool-blazer",
                    ShortDescription = "A structured blazer cut from soft wool-blend.",
                    Description = "Elevate your wardrobe with this tailored wool-blend blazer, featuring a structured shoulder, notch lapel, and a single-button closure. Fully lined for a polished finish.",
                    Price = 429.00m,
                    CompareAtPrice = 549.00m,
                    Sku = "WB-BLZ-001",
                    StockQuantity = 24,
                    Sizes = "XS,S,M,L,XL",
                    Colors = "Black,Camel",
                    Material = "70% Wool, 30% Polyester",
                    IsFeatured = true,
                    IsNewArrival = true,
                    CategoryId = women.Id,
                    MainImageUrl = "/images/products/blazer.svg"
                },
                new()
                {
                    Name = "Silk Slip Dress",
                    Slug = "silk-slip-dress",
                    ShortDescription = "Bias-cut slip dress in pure mulberry silk.",
                    Description = "A timeless bias-cut slip dress crafted from 100% mulberry silk. Adjustable straps and a fluid drape make this an effortless evening staple.",
                    Price = 349.00m,
                    Sku = "WM-DRS-002",
                    StockQuantity = 15,
                    Sizes = "XS,S,M,L",
                    Colors = "Ivory,Emerald,Black",
                    Material = "100% Mulberry Silk",
                    IsFeatured = true,
                    CategoryId = women.Id,
                    MainImageUrl = "/images/products/slip-dress.svg"
                },
                new()
                {
                    Name = "Ribbed Knit Sweater",
                    Slug = "ribbed-knit-sweater",
                    ShortDescription = "Soft ribbed sweater for layering.",
                    Description = "A relaxed-fit ribbed knit sweater made from a cotton-cashmere blend, perfect for cooler evenings and easy layering.",
                    Price = 189.00m,
                    Sku = "WM-KNT-003",
                    StockQuantity = 40,
                    Sizes = "XS,S,M,L,XL",
                    Colors = "Oatmeal,Charcoal,Rust",
                    Material = "85% Cotton, 15% Cashmere",
                    IsNewArrival = true,
                    CategoryId = women.Id,
                    MainImageUrl = "/images/products/sweater.svg"
                },
                new()
                {
                    Name = "Classic Oxford Shirt",
                    Slug = "classic-oxford-shirt",
                    ShortDescription = "Crisp cotton Oxford shirt, tailored fit.",
                    Description = "An essential Oxford shirt made from breathable cotton, cut for a tailored fit with a classic point collar and button cuffs.",
                    Price = 159.00m,
                    Sku = "MN-SHT-001",
                    StockQuantity = 60,
                    Sizes = "S,M,L,XL,XXL",
                    Colors = "White,Sky Blue,Light Grey",
                    Material = "100% Cotton",
                    IsFeatured = true,
                    CategoryId = men.Id,
                    MainImageUrl = "/images/products/oxford-shirt.svg"
                },
                new()
                {
                    Name = "Slim Fit Chinos",
                    Slug = "slim-fit-chinos",
                    ShortDescription = "Stretch-cotton chinos with a modern taper.",
                    Description = "Versatile slim-fit chinos made from stretch cotton twill, tailored with a modern taper and finished with a durable YKK zip.",
                    Price = 199.00m,
                    CompareAtPrice = 249.00m,
                    Sku = "MN-PNT-002",
                    StockQuantity = 45,
                    Sizes = "30,32,34,36,38",
                    Colors = "Khaki,Navy,Black",
                    Material = "98% Cotton, 2% Elastane",
                    CategoryId = men.Id,
                    MainImageUrl = "/images/products/chinos.svg"
                },
                new()
                {
                    Name = "Merino Wool Overcoat",
                    Slug = "merino-wool-overcoat",
                    ShortDescription = "Double-breasted overcoat in merino wool.",
                    Description = "A double-breasted overcoat crafted from premium merino wool, offering warmth and a refined silhouette for the colder months.",
                    Price = 699.00m,
                    Sku = "MN-COT-003",
                    StockQuantity = 12,
                    Sizes = "S,M,L,XL",
                    Colors = "Charcoal,Camel",
                    Material = "90% Merino Wool, 10% Nylon",
                    IsFeatured = true,
                    IsNewArrival = true,
                    CategoryId = men.Id,
                    MainImageUrl = "/images/products/overcoat.svg"
                },
                new()
                {
                    Name = "Leather Crossbody Bag",
                    Slug = "leather-crossbody-bag",
                    ShortDescription = "Full-grain leather bag with adjustable strap.",
                    Description = "A compact crossbody bag crafted from full-grain leather, featuring an adjustable strap and a secure magnetic closure.",
                    Price = 259.00m,
                    Sku = "AC-BAG-001",
                    StockQuantity = 20,
                    Colors = "Tan,Black",
                    Material = "Full-grain Leather",
                    IsFeatured = true,
                    CategoryId = accessories.Id,
                    MainImageUrl = "/images/products/bag.svg"
                },
                new()
                {
                    Name = "Minimalist Leather Belt",
                    Slug = "minimalist-leather-belt",
                    ShortDescription = "Reversible leather belt, brushed buckle.",
                    Description = "A reversible leather belt with a brushed metal buckle, offering two colourways in a single accessory.",
                    Price = 89.00m,
                    Sku = "AC-BLT-002",
                    StockQuantity = 50,
                    Sizes = "S,M,L",
                    Colors = "Black/Brown",
                    Material = "Genuine Leather",
                    CategoryId = accessories.Id,
                    MainImageUrl = "/images/products/belt.svg"
                },
                new()
                {
                    Name = "Suede Chelsea Boots",
                    Slug = "suede-chelsea-boots",
                    ShortDescription = "Classic Chelsea boots in soft suede.",
                    Description = "Timeless Chelsea boots crafted from soft suede with elastic side panels and a durable rubber sole for all-day comfort.",
                    Price = 379.00m,
                    CompareAtPrice = 459.00m,
                    Sku = "FW-BOT-001",
                    StockQuantity = 18,
                    Sizes = "40,41,42,43,44,45",
                    Colors = "Sand,Black",
                    Material = "Suede Leather",
                    IsNewArrival = true,
                    CategoryId = footwear.Id,
                    MainImageUrl = "/images/products/boots.svg"
                },
                new()
                {
                    Name = "Canvas Low-Top Sneakers",
                    Slug = "canvas-low-top-sneakers",
                    ShortDescription = "Everyday canvas sneakers with rubber sole.",
                    Description = "Lightweight canvas sneakers built for everyday wear, with a cushioned insole and a durable vulcanized rubber sole.",
                    Price = 149.00m,
                    Sku = "FW-SNK-002",
                    StockQuantity = 55,
                    Sizes = "38,39,40,41,42,43,44",
                    Colors = "White,Black,Off-White",
                    Material = "Canvas, Rubber",
                    IsFeatured = true,
                    CategoryId = footwear.Id,
                    MainImageUrl = "/images/products/sneakers.svg"
                }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }

        // Demo coupons
        if (!await context.Coupons.AnyAsync())
        {
            context.Coupons.AddRange(
                new Coupon
                {
                    Code = "WELCOME10",
                    Description = "10% off your first order",
                    Type = CouponType.Percentage,
                    Value = 10,
                    MinOrderAmount = 100,
                    ExpiresAt = DateTime.UtcNow.AddMonths(6)
                },
                new Coupon
                {
                    Code = "FLAT50",
                    Description = "AED 50 off orders over AED 300",
                    Type = CouponType.FixedAmount,
                    Value = 50,
                    MinOrderAmount = 300,
                    MaxDiscountAmount = 50,
                    ExpiresAt = DateTime.UtcNow.AddMonths(3)
                }
            );
            await context.SaveChangesAsync();
        }
    }
}
