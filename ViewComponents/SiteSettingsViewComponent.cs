using ApparelShop.Data;
using ApparelShop.Models;
using ApparelShop.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApparelShop.ViewComponents;

public class SiteSettingsViewComponent : ViewComponent
{
    private readonly SiteSettingsService _settingsService;
    private readonly ApplicationDbContext _context;

    public SiteSettingsViewComponent(SiteSettingsService settingsService, ApplicationDbContext context)
    {
        _settingsService = settingsService;
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync(string section = "Header")
    {
        var settings = await _settingsService.GetAsync();

        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive && c.ParentCategoryId == null)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        ViewBag.Categories = categories;
        return View(section, settings);
    }
}
