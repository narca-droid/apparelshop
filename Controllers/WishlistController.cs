using System.Text.Json;
using ApparelShop.Models;
using Microsoft.AspNetCore.Mvc;

namespace ApparelShop.Controllers;

public class WishlistController : Controller
{
    private const string SessionKey = "WishlistV1";

    private Wishlist GetWishlist()
    {
        var json = HttpContext.Session.GetString(SessionKey);
        if (string.IsNullOrEmpty(json)) return new Wishlist();
        return JsonSerializer.Deserialize<Wishlist>(json) ?? new Wishlist();
    }

    private void SaveWishlist(Wishlist wl)
    {
        HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(wl));
    }

    public IActionResult Index()
    {
        return View(GetWishlist());
    }

    [HttpGet("/Wishlist/Count")]
    public IActionResult Count()
    {
        return Json(new { count = GetWishlist().TotalItems });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Add(int productId, string productName, string? slug, decimal price, decimal? compareAtPrice, string? imageUrl)
    {
        var wl = GetWishlist();
        if (wl.Items.Any(i => i.ProductId == productId))
        {
            TempData["Success"] = $"{productName} is already in your wishlist.";
            return Redirect(Request.Headers["Referer"].ToString() ?? "/shop");
        }

        wl.Items.Add(new WishlistItem
        {
            ProductId = productId,
            ProductName = productName,
            Slug = slug,
            Price = price,
            CompareAtPrice = compareAtPrice,
            ImageUrl = imageUrl
        });
        SaveWishlist(wl);
        TempData["Success"] = $"{productName} added to your wishlist.";
        return Redirect(Request.Headers["Referer"].ToString() ?? "/shop");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int productId)
    {
        var wl = GetWishlist();
        var item = wl.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            wl.Items.Remove(item);
            SaveWishlist(wl);
            TempData["Success"] = $"{item.ProductName} removed from your wishlist.";
        }
        return RedirectToAction("Index");
    }
}
