using System.Text.Json;
using ApparelShop.Data;
using ApparelShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApparelShop.Controllers;

public class CartController : Controller
{
    private const string SessionKey = "CartV1";
    private const string RecentlyViewedKey = "RecentlyViewed";
    private readonly ApplicationDbContext _context;

    public CartController(ApplicationDbContext context)
    {
        _context = context;
    }

    private Cart GetCart()
    {
        var json = HttpContext.Session.GetString(SessionKey);
        if (string.IsNullOrEmpty(json)) return new Cart();
        return JsonSerializer.Deserialize<Cart>(json) ?? new Cart();
    }

    private void SaveCart(Cart cart)
    {
        HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(cart));
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

    public IActionResult Index()
    {
        return View(GetCart());
    }

    [HttpGet("/Cart/Count")]
    public IActionResult Count()
    {
        return Json(new { count = GetCart().TotalQuantity });
    }

    [HttpGet("/Cart/Summary")]
    public IActionResult Summary()
    {
        var cart = GetCart();
        return Json(new
        {
            items = cart.Items.Select(i => new
            {
                i.ProductId,
                i.ProductName,
                i.ImageUrl,
                i.UnitPrice,
                i.Size,
                i.Color,
                i.Quantity,
                i.LineTotal
            }),
            subtotal = cart.Subtotal,
            totalQuantity = cart.TotalQuantity
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, string? size, string? color, int quantity = 1)
    {
        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId && p.IsActive && !p.IsDeleted);
        if (product is null) return NotFound();

        if (quantity < 1) quantity = 1;

        if (product.StockQuantity < 1)
        {
            if (IsAjaxRequest()) return Json(new { success = false, message = $"{product.Name} is out of stock." });
            TempData["Error"] = $"{product.Name} is out of stock.";
            return RedirectToAction("Index", "Shop", new { slug = product.Slug });
        }

        var cart = GetCart();
        var existing = cart.Items.FirstOrDefault(i => i.ProductId == productId && i.Size == size && i.Color == color);
        var currentQtyInCart = existing?.Quantity ?? 0;

        if (currentQtyInCart + quantity > product.StockQuantity)
        {
            var msg = $"Only {product.StockQuantity} units of {product.Name} available. You already have {currentQtyInCart} in your bag.";
            if (IsAjaxRequest()) return Json(new { success = false, message = msg });
            TempData["Error"] = msg;
            return RedirectToAction("Index", "Shop", new { slug = product.Slug });
        }

        if (existing is not null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ImageUrl = product.MainImageUrl,
                UnitPrice = product.Price,
                Size = size,
                Color = color,
                Quantity = quantity
            });
        }

        SaveCart(cart);

        if (IsAjaxRequest())
        {
            return Json(new
            {
                success = true,
                message = $"{product.Name} added to your bag.",
                cartCount = cart.TotalQuantity,
                items = cart.Items.Select(i => new
                {
                    i.ProductId,
                    i.ProductName,
                    i.ImageUrl,
                    i.Size,
                    i.Color,
                    i.Quantity,
                    i.LineTotal
                }),
                subtotal = cart.Subtotal
            });
        }

        TempData["Success"] = $"{product.Name} added to your bag.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(int productId, string? size, string? color, int quantity)
    {
        var cart = GetCart();
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId && i.Size == size && i.Color == color);
        if (item is not null)
        {
            if (quantity <= 0) cart.Items.Remove(item);
            else
            {
                var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
                if (product != null && quantity > product.StockQuantity)
                    quantity = product.StockQuantity;
                item.Quantity = quantity;
            }
        }
        SaveCart(cart);

        if (IsAjaxRequest())
        {
            return Json(new
            {
                success = true,
                cartCount = cart.TotalQuantity,
                subtotal = cart.Subtotal,
                items = cart.Items.Select(i => new
                {
                    i.ProductId,
                    i.ProductName,
                    i.ImageUrl,
                    i.Size,
                    i.Color,
                    i.Quantity,
                    i.LineTotal
                })
            });
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int productId, string? size, string? color)
    {
        var cart = GetCart();
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId && i.Size == size && i.Color == color);
        if (item is not null) cart.Items.Remove(item);
        SaveCart(cart);

        if (IsAjaxRequest())
        {
            return Json(new
            {
                success = true,
                message = "Item removed from your bag.",
                cartCount = cart.TotalQuantity,
                subtotal = cart.Subtotal,
                items = cart.Items.Select(i => new
                {
                    i.ProductId,
                    i.ProductName,
                    i.ImageUrl,
                    i.Size,
                    i.Color,
                    i.Quantity,
                    i.LineTotal
                })
            });
        }

        TempData["Success"] = "Item removed from your bag.";
        return RedirectToAction("Index");
    }

    [HttpGet("/RecentlyViewed")]
    public async Task<IActionResult> RecentlyViewed()
    {
        var json = HttpContext.Session.GetString(RecentlyViewedKey);
        if (string.IsNullOrEmpty(json)) return Json(Array.Empty<object>());

        var ids = JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
        if (!ids.Any()) return Json(Array.Empty<object>());

        var products = await _context.Products
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id) && p.IsActive && !p.IsDeleted)
            .ToListAsync();

        var ordered = ids
            .Select(id => products.FirstOrDefault(p => p.Id == id))
            .Where(p => p != null)
            .Select(p => new
            {
                p!.Id,
                p.Name,
                p.Slug,
                p.Price,
                p.CompareAtPrice,
                p.MainImageUrl,
                p.OnSale,
                p.DiscountPercent
            });

        return Json(ordered);
    }

    private bool IsAjaxRequest()
    {
        return Request.Headers.XRequestedWith == "XMLHttpRequest"
            || Request.Headers.Accept.ToString().Contains("application/json");
    }
}
