using ApparelShop.Data;
using ApparelShop.Models;
using ApparelShop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApparelShop.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class SettingsController : AdminBaseController
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly SiteSettingsService _settingsService;
    private readonly AuditService _auditService;

    public SettingsController(ApplicationDbContext context, IWebHostEnvironment env, SiteSettingsService settingsService, AuditService auditService)
        : base(env)
    {
        _context = context;
        _env = env;
        _settingsService = settingsService;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index()
    {
        var settings = await _settingsService.GetAsync();
        return View(settings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SiteSetting model, IFormFile? logo, IFormFile? favicon, IFormFile? heroImage, bool removeLogo = false)
    {
        if (!ModelState.IsValid) return View(model);

        var settings = await _context.SiteSettings.FirstOrDefaultAsync();
        if (settings is null)
        {
            settings = new SiteSetting();
            _context.SiteSettings.Add(settings);
        }

        settings.SiteName = model.SiteName;
        settings.Tagline = model.Tagline;
        settings.PrimaryColor = model.PrimaryColor;
        settings.AccentColor = model.AccentColor;
        settings.BackgroundColor = model.BackgroundColor;
        settings.HeroHeadline = model.HeroHeadline;
        settings.HeroSubtext = model.HeroSubtext;
        settings.HeroButtonText = model.HeroButtonText;
        settings.HeroButtonUrl = model.HeroButtonUrl;
        settings.ContactEmail = model.ContactEmail;
        settings.ContactPhone = model.ContactPhone;
        settings.Address = model.Address;
        settings.FacebookUrl = model.FacebookUrl;
        settings.InstagramUrl = model.InstagramUrl;
        settings.TiktokUrl = model.TiktokUrl;
        settings.FooterAboutText = model.FooterAboutText;
        settings.ShowNewArrivalsSection = model.ShowNewArrivalsSection;
        settings.ShowFeaturedSection = model.ShowFeaturedSection;
        settings.MetaTitle = model.MetaTitle;
        settings.MetaDescription = model.MetaDescription;
        settings.UpdatedAt = DateTime.UtcNow;

        if (removeLogo)
        {
            settings.LogoUrl = null;
        }
        else if (logo is { Length: > 0 })
        {
            settings.LogoUrl = await SaveUploadAsync(logo);
        }
        if (favicon is { Length: > 0 }) settings.FaviconUrl = await SaveUploadAsync(favicon);
        if (heroImage is { Length: > 0 }) settings.HeroImageUrl = await SaveUploadAsync(heroImage);

        await _context.SaveChangesAsync();
        _settingsService.Invalidate();
        await _auditService.LogAsync("Update", "SiteSettings", details: "Site settings updated");
        TempData["Success"] = "Site settings updated. Changes are live immediately.";
        return RedirectToAction(nameof(Index));
    }
}
