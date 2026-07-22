using ApparelShop.Data;
using ApparelShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApparelShop.Controllers;

public class NewsletterController : Controller
{
    private readonly ApplicationDbContext _context;

    public NewsletterController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscribe(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            if (IsAjaxRequest()) return Json(new { success = false, message = "Please enter your email." });
            TempData["Error"] = "Please enter your email.";
            return RedirectToAction("Index", "Home");
        }

        var existing = await _context.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.Email.ToLower() == email.ToLower());

        if (existing is not null)
        {
            if (IsAjaxRequest()) return Json(new { success = true, message = "You're already subscribed!" });
            TempData["Success"] = "You're already subscribed!";
            return RedirectToAction("Index", "Home");
        }

        _context.NewsletterSubscribers.Add(new NewsletterSubscriber { Email = email });
        await _context.SaveChangesAsync();

        if (IsAjaxRequest()) return Json(new { success = true, message = "Thanks for subscribing!" });
        TempData["Success"] = "Thanks for subscribing!";
        return RedirectToAction("Index", "Home");
    }

    private bool IsAjaxRequest()
    {
        return Request.Headers.XRequestedWith == "XMLHttpRequest"
            || Request.Headers.Accept.ToString().Contains("application/json");
    }
}
