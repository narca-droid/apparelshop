using ApparelShop.Data;
using ApparelShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApparelShop.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ReviewsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReviewsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? filter, int page = 1)
    {
        const int pageSize = 20;
        var query = _context.ProductReviews
            .Include(r => r.Product)
            .AsQueryable();

        if (filter == "pending")
            query = query.Where(r => !r.IsApproved);
        else if (filter == "approved")
            query = query.Where(r => r.IsApproved);

        var total = await query.CountAsync();
        var reviews = await query.OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.Filter = filter;
        ViewBag.PendingCount = await _context.ProductReviews.CountAsync(r => !r.IsApproved);
        ViewBag.TotalCount = await _context.ProductReviews.CountAsync();

        return View(reviews);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var review = await _context.ProductReviews.FindAsync(id);
        if (review is null) return NotFound();

        review.IsApproved = true;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Review approved and is now visible on the product page.";
        return RedirectToAction("Index", new { filter = "pending" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await _context.ProductReviews.FindAsync(id);
        if (review is null) return NotFound();

        _context.ProductReviews.Remove(review);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Review deleted.";
        return RedirectToAction("Index");
    }
}
