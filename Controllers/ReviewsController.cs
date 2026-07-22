using ApparelShop.Data;
using ApparelShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApparelShop.Controllers;

public class ReviewsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReviewsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, string reviewerName, string reviewerEmail, int rating, string title, string? body)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product is null) return NotFound();

        if (string.IsNullOrWhiteSpace(reviewerName) || string.IsNullOrWhiteSpace(reviewerEmail) ||
            string.IsNullOrWhiteSpace(title) || rating < 1 || rating > 5)
        {
            TempData["Error"] = "Please fill in all required fields and select a rating.";
            return Redirect($"/shop/product/{product.Slug}");
        }

        var review = new ProductReview
        {
            ProductId = productId,
            ReviewerName = reviewerName,
            ReviewerEmail = reviewerEmail,
            Rating = rating,
            Title = title,
            Body = body,
            IsApproved = false
        };

        _context.ProductReviews.Add(review);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Thank you! Your review has been submitted and will appear after approval.";
        return Redirect($"/shop/product/{product.Slug}#reviews");
    }
}
