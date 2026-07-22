using ApparelShop.Data;
using ApparelShop.Models;
using ApparelShop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApparelShop.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoriesController : AdminBaseController
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly AuditService _auditService;

    public CategoriesController(ApplicationDbContext context, IWebHostEnvironment env, AuditService auditService)
        : base(env)
    {
        _context = context;
        _env = env;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _context.Categories
            .Include(c => c.Products)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
        return View(categories);
    }

    public IActionResult Create() => View(new Category());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category category, IFormFile? image)
    {
        if (string.IsNullOrWhiteSpace(category.Slug))
            category.Slug = Slugify(category.Name);

        if (await _context.Categories.AnyAsync(c => c.Slug == category.Slug))
            ModelState.AddModelError(nameof(category.Slug), "This slug is already in use.");

        if (!ModelState.IsValid) return View(category);

        if (image is { Length: > 0 })
            category.ImageUrl = await SaveUploadAsync(image);

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Category", category.Id, $"Created category '{category.Name}'");
        TempData["Success"] = "Category created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category is null) return NotFound();
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category category, IFormFile? image)
    {
        if (id != category.Id) return NotFound();

        var existing = await _context.Categories.FindAsync(id);
        if (existing is null) return NotFound();

        if (string.IsNullOrWhiteSpace(category.Slug))
            category.Slug = Slugify(category.Name);

        if (await _context.Categories.AnyAsync(c => c.Slug == category.Slug && c.Id != id))
            ModelState.AddModelError(nameof(category.Slug), "This slug is already in use.");

        if (!ModelState.IsValid) return View(category);

        existing.Name = category.Name;
        existing.Slug = category.Slug;
        existing.Description = category.Description;
        existing.DisplayOrder = category.DisplayOrder;
        existing.IsActive = category.IsActive;

        if (image is { Length: > 0 })
            existing.ImageUrl = await SaveUploadAsync(image);

        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Update", "Category", id, $"Updated category '{category.Name}'");
        TempData["Success"] = "Category updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category is null) return RedirectToAction(nameof(Index));

        var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id && !p.IsDeleted);
        if (hasProducts)
        {
            // Soft-delete: deactivate instead of removing
            category.IsActive = false;
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Deactivate", "Category", id, $"Deactivated category '{category.Name}' (has products)");
            TempData["Success"] = "Category deactivated (it still has products).";
        }
        else
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Delete", "Category", id, $"Deleted category '{category.Name}'");
            TempData["Success"] = "Category deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    private static string Slugify(string name)
    {
        var slug = name.ToLowerInvariant().Trim();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");
        return slug.Trim('-');
    }
}
