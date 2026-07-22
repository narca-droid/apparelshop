using System.Text.Json;
using ApparelShop.Data;
using ApparelShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApparelShop.Controllers;

public class ShopController : Controller
{
    private const string RecentlyViewedKey = "RecentlyViewed";
    private readonly ApplicationDbContext _context;
    private const int PageSize = 12;

    public ShopController(ApplicationDbContext context)
    {
        _context = context;
    }

    private void TrackRecentlyViewed(int productId)
    {
        var json = HttpContext.Session.GetString(RecentlyViewedKey);
        var ids = string.IsNullOrEmpty(json)
            ? new List<int>()
            : JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
        ids.Remove(productId);
        ids.Insert(0, productId);
        if (ids.Count > 20) ids = ids.Take(20).ToList();
        HttpContext.Session.SetString(RecentlyViewedKey, JsonSerializer.Serialize(ids));
    }

    public async Task<IActionResult> Index(string? category, string? q, string? filter, string? sort, decimal? minPrice, decimal? maxPrice, int page = 1)
    {
        var query = _context.Products.AsNoTracking().Include(p => p.Category)
            .Where(p => p.IsActive && !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category!.Slug == category);
            ViewBag.CurrentCategory = category;
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(p => p.Name.Contains(q) || (p.ShortDescription != null && p.ShortDescription.Contains(q)));
            ViewBag.SearchTerm = q;
        }

        if (filter == "new") query = query.Where(p => p.IsNewArrival);
        if (filter == "featured") query = query.Where(p => p.IsFeatured);
        if (filter == "sale") query = query.Where(p => p.CompareAtPrice != null && p.CompareAtPrice > p.Price);

        if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);

        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;

        var priceRange = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted)
            .Select(p => new { p.Price })
            .ToListAsync();
        ViewBag.PriceMin = priceRange.Any() ? priceRange.Min(p => p.Price) : 0m;
        ViewBag.PriceMax = priceRange.Any() ? priceRange.Max(p => p.Price) : 0m;

        query = sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name" => query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var products = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        ViewBag.Sort = sort;
        ViewBag.Filter = filter;

        return View(products);
    }

    [Route("/shop/product/{slug}")]
    public async Task<IActionResult> Product(string slug)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive && !p.IsDeleted);

        if (product is null) return NotFound();

        TrackRecentlyViewed(product.Id);

        ViewBag.Related = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted && p.CategoryId == product.CategoryId && p.Id != product.Id)
            .Take(4)
            .ToListAsync();

        ViewBag.Reviews = await _context.ProductReviews
            .AsNoTracking()
            .Where(r => r.ProductId == product.Id && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        ViewBag.AverageRating = await _context.ProductReviews
            .AsNoTracking()
            .Where(r => r.ProductId == product.Id && r.IsApproved)
            .AverageAsync(r => (double?)r.Rating) ?? 0;

        ViewBag.ReviewCount = await _context.ProductReviews
            .AsNoTracking()
            .CountAsync(r => r.ProductId == product.Id && r.IsApproved);

        ViewBag.NotifyCount = await _context.StockNotifications
            .AsNoTracking()
            .CountAsync(sn => sn.ProductId == product.Id && !sn.IsNotified);

        return View(product);
    }

    [HttpGet("/Shop/SearchSuggestions")]
    public async Task<IActionResult> SearchSuggestions(string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Json(Array.Empty<object>());

        var suggestions = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted && p.Name.Contains(q))
            .Select(p => new { p.Name, p.Slug, p.Price, p.MainImageUrl })
            .Take(6)
            .ToListAsync();

        return Json(suggestions);
    }
}
