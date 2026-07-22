using System.Text.Json;
using ApparelShop.Data;
using ApparelShop.Models;
using ApparelShop.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApparelShop.Controllers;

public class HomeController : Controller
{
    private const string RecentlyViewedKey = "RecentlyViewed";
    private readonly ApplicationDbContext _context;
    private readonly SiteSettingsService _settingsService;

    public HomeController(ApplicationDbContext context, SiteSettingsService settingsService)
    {
        _context = context;
        _settingsService = settingsService;
    }

    public async Task<IActionResult> Index()
    {
        var settings = await _settingsService.GetAsync();
        ViewBag.Settings = settings;

        ViewBag.Categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive && c.ParentCategoryId == null)
            .OrderBy(c => c.DisplayOrder)
            .Take(4)
            .ToListAsync();

        if (settings.ShowFeaturedSection)
        {
            ViewBag.Featured = await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive && !p.IsDeleted && p.IsFeatured)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync();
        }

        if (settings.ShowNewArrivalsSection)
        {
            ViewBag.NewArrivals = await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive && !p.IsDeleted && p.IsNewArrival)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync();
        }

        var rvJson = HttpContext.Session.GetString(RecentlyViewedKey);
        if (!string.IsNullOrEmpty(rvJson))
        {
            var rvIds = JsonSerializer.Deserialize<List<int>>(rvJson) ?? new List<int>();
            if (rvIds.Any())
            {
                ViewBag.RecentlyViewed = await _context.Products
                    .AsNoTracking()
                    .Where(p => rvIds.Contains(p.Id) && p.IsActive && !p.IsDeleted)
                    .ToListAsync();
            }
        }

        return View();
    }

    [Route("/page/{key}")]
    public async Task<IActionResult> Page(string key)
    {
        var block = await _context.ContentBlocks
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Key == key && b.IsActive);

        if (block is null) return NotFound();

        return View(block);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
