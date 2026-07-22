using ApparelShop.Data;
using ApparelShop.Models;
using ApparelShop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApparelShop.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductsController : AdminBaseController
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly AuditService _auditService;

    public ProductsController(ApplicationDbContext context, IWebHostEnvironment env, AuditService auditService)
        : base(env)
    {
        _context = context;
        _env = env;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        const int pageSize = 15;
        var query = _context.Products.Include(p => p.Category).Where(p => !p.IsDeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Name.Contains(q) || p.Sku.Contains(q));

        var total = await query.CountAsync();
        var products = await query.OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.Query = q;

        return View(products);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        return View(new Product());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product, IFormFile? mainImage, List<IFormFile>? galleryImages)
    {
        if (string.IsNullOrWhiteSpace(product.Slug))
            product.Slug = Slugify(product.Name);

        if (await _context.Products.AnyAsync(p => p.Slug == product.Slug))
            ModelState.AddModelError(nameof(product.Slug), "This URL slug is already in use.");

        if (await _context.Products.AnyAsync(p => p.Sku == product.Sku))
            ModelState.AddModelError(nameof(product.Sku), "This SKU already exists.");

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(product);
        }

        if (mainImage is { Length: > 0 })
            product.MainImageUrl = await SaveUploadAsync(mainImage);

        product.CreatedAt = DateTime.UtcNow;
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        if (galleryImages is { Count: > 0 })
        {
            int order = 0;
            foreach (var file in galleryImages.Where(f => f.Length > 0))
            {
                var url = await SaveUploadAsync(file);
                _context.ProductImages.Add(new ProductImage { ProductId = product.Id, ImageUrl = url, DisplayOrder = order++, AltText = product.Name });
            }
            await _context.SaveChangesAsync();
        }

        await _auditService.LogAsync("Create", "Product", product.Id, $"Created product '{product.Name}'");
        TempData["Success"] = "Product created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await _context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound();
        ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product product, IFormFile? mainImage, List<IFormFile>? galleryImages)
    {
        if (id != product.Id) return NotFound();

        var existing = await _context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
        if (existing is null) return NotFound();

        if (string.IsNullOrWhiteSpace(product.Slug))
            product.Slug = Slugify(product.Name);

        if (await _context.Products.AnyAsync(p => p.Slug == product.Slug && p.Id != id))
            ModelState.AddModelError(nameof(product.Slug), "This URL slug is already in use.");

        if (await _context.Products.AnyAsync(p => p.Sku == product.Sku && p.Id != id))
            ModelState.AddModelError(nameof(product.Sku), "This SKU already exists.");

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            product.Images = existing.Images;
            return View(product);
        }

        existing.Name = product.Name;
        existing.Slug = product.Slug;
        existing.Description = product.Description;
        existing.ShortDescription = product.ShortDescription;
        existing.Price = product.Price;
        existing.CompareAtPrice = product.CompareAtPrice;
        existing.Sku = product.Sku;
        existing.StockQuantity = product.StockQuantity;
        existing.Sizes = product.Sizes;
        existing.Colors = product.Colors;
        existing.Material = product.Material;
        existing.IsFeatured = product.IsFeatured;
        existing.IsActive = product.IsActive;
        existing.IsNewArrival = product.IsNewArrival;
        existing.CategoryId = product.CategoryId;
        existing.UpdatedAt = DateTime.UtcNow;

        if (mainImage is { Length: > 0 })
            existing.MainImageUrl = await SaveUploadAsync(mainImage);

        if (galleryImages is { Count: > 0 })
        {
            int order = existing.Images.Count;
            foreach (var file in galleryImages.Where(f => f.Length > 0))
            {
                var url = await SaveUploadAsync(file);
                _context.ProductImages.Add(new ProductImage { ProductId = existing.Id, ImageUrl = url, DisplayOrder = order++, AltText = existing.Name });
            }
        }

        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Update", "Product", id, $"Updated product '{product.Name}'");
        TempData["Success"] = "Product updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int imageId, int productId)
    {
        var image = await _context.ProductImages.FindAsync(imageId);
        if (image is not null)
        {
            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null) return NotFound();

        var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
        if (hasOrders)
        {
            product.IsActive = false;
            product.IsDeleted = true;
            TempData["Success"] = "Product has past orders, so it was deactivated instead of deleted.";
        }
        else
        {
            _context.Products.Remove(product);
            TempData["Success"] = "Product deleted.";
        }

        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Delete", "Product", id, $"Deleted product '{product.Name}'");
        return RedirectToAction(nameof(Index));
    }

    private static string Slugify(string name)
    {
        var slug = name.ToLowerInvariant().Trim();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");
        return slug.Trim('-') + "-" + Guid.NewGuid().ToString("N")[..4];
    }
}
