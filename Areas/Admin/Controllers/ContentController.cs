using ApparelShop.Data;
using ApparelShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApparelShop.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ContentController : Controller
{
    private readonly ApplicationDbContext _context;

    public ContentController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var blocks = await _context.ContentBlocks.OrderBy(b => b.Title).ToListAsync();
        return View(blocks);
    }

    public IActionResult Create() => View(new ContentBlock());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ContentBlock block)
    {
        block.Key = Slugify(block.Key.Length > 0 ? block.Key : block.Title);

        if (await _context.ContentBlocks.AnyAsync(b => b.Key == block.Key))
            ModelState.AddModelError(nameof(block.Key), "This key is already used by another page.");

        if (!ModelState.IsValid) return View(block);

        block.UpdatedAt = DateTime.UtcNow;
        _context.ContentBlocks.Add(block);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Content page created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var block = await _context.ContentBlocks.FindAsync(id);
        if (block is null) return NotFound();
        return View(block);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ContentBlock block)
    {
        if (id != block.Id) return NotFound();
        var existing = await _context.ContentBlocks.FindAsync(id);
        if (existing is null) return NotFound();

        existing.Title = block.Title;
        existing.BodyHtml = block.BodyHtml;
        existing.IsActive = block.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Content page updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var block = await _context.ContentBlocks.FindAsync(id);
        if (block is not null)
        {
            _context.ContentBlocks.Remove(block);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Content page deleted.";
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
