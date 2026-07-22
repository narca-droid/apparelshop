using ApparelShop.Data;
using ApparelShop.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ApparelShop.Services;

public class SiteSettingsService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "SiteSettings_v1";

    public SiteSettingsService(ApplicationDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<SiteSetting> GetAsync()
    {
        if (_cache.TryGetValue(CacheKey, out SiteSetting? cached) && cached is not null)
            return cached;

        var settings = await _context.SiteSettings.AsNoTracking().FirstOrDefaultAsync()
            ?? new SiteSetting();

        _cache.Set(CacheKey, settings, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
            SlidingExpiration = TimeSpan.FromMinutes(2)
        });

        return settings;
    }

    public void Invalidate()
    {
        _cache.Remove(CacheKey);
    }
}
