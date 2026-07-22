using ApparelShop.Data;
using ApparelShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApparelShop.Controllers;

public class StockNotificationController : Controller
{
    private readonly ApplicationDbContext _context;

    public StockNotificationController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscribe(int productId, string email)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product is null) return NotFound();

        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Please enter a valid email address.";
            return Redirect($"/shop/product/{product.Slug}");
        }

        var existing = await _context.StockNotifications
            .AnyAsync(sn => sn.ProductId == productId && sn.Email == email && !sn.IsNotified);

        if (existing)
        {
            TempData["Success"] = "You're already on the notification list for this product.";
            return Redirect($"/shop/product/{product.Slug}");
        }

        _context.StockNotifications.Add(new StockNotification
        {
            ProductId = productId,
            Email = email
        });
        await _context.SaveChangesAsync();

        TempData["Success"] = "You'll be notified when this product is back in stock!";
        return Redirect($"/shop/product/{product.Slug}");
    }
}
