using ApparelShop.Data;
using ApparelShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApparelShop.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? range)
    {
        var startDate = range switch
        {
            "today" => DateTime.UtcNow.Date,
            "week" => DateTime.UtcNow.AddDays(-7),
            "month" => DateTime.UtcNow.AddMonths(-1),
            "quarter" => DateTime.UtcNow.AddMonths(-3),
            "year" => DateTime.UtcNow.AddYears(-1),
            _ => (DateTime?)null
        };

        ViewBag.SelectedRange = range ?? "all";
        ViewBag.ProductCount = await _context.Products.CountAsync(p => !p.IsDeleted);
        ViewBag.CategoryCount = await _context.Categories.CountAsync();
        ViewBag.OrderCount = await _context.Orders.CountAsync();
        ViewBag.PendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending);

        var revenueQuery = _context.Orders
            .Where(o => o.Status != OrderStatus.Cancelled);

        if (startDate.HasValue)
            revenueQuery = revenueQuery.Where(o => o.CreatedAt >= startDate.Value);

        var revenueValues = await revenueQuery.Select(o => o.Total).ToListAsync();
        ViewBag.Revenue = revenueValues.Count > 0 ? revenueValues.Sum() : 0m;

        var lowStockQuery = _context.Products.Where(p => !p.IsDeleted);
        if (startDate.HasValue)
            lowStockQuery = lowStockQuery.Where(p => p.CreatedAt >= startDate.Value);
        ViewBag.LowStock = await lowStockQuery.CountAsync(p => p.StockQuantity <= 5);

        var ordersQuery = _context.Orders.AsQueryable();
        if (startDate.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedAt >= startDate.Value);

        ViewBag.RecentOrders = await ordersQuery
            .OrderByDescending(o => o.CreatedAt)
            .Take(6)
            .ToListAsync();

        return View();
    }
}
