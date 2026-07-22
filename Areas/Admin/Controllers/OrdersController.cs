using ApparelShop.Data;
using ApparelShop.Models;
using ApparelShop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApparelShop.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _auditService;

    public OrdersController(ApplicationDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? status)
    {
        var query = _context.Orders.AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var parsed))
            query = query.Where(o => o.Status == parsed);

        ViewBag.StatusFilter = status;
        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return View(orders);
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return NotFound();
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, string? confirmationNotes)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order is null) return NotFound();

        if (order.Status != OrderStatus.Pending)
        {
            TempData["Error"] = "Only pending orders can be confirmed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        order.Status = OrderStatus.Confirmed;
        order.ConfirmedAt = DateTime.UtcNow;
        order.ConfirmationNotes = confirmationNotes;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Confirm", "Order", id, $"Order {order.OrderNumber} confirmed by phone call");
        TempData["Success"] = $"Order {order.OrderNumber} confirmed. Ready for processing.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? confirmationNotes)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order is null) return NotFound();

        if (order.Status != OrderStatus.Pending)
        {
            TempData["Error"] = "Only pending orders can be rejected.";
            return RedirectToAction(nameof(Details), new { id });
        }

        order.Status = OrderStatus.Cancelled;
        order.ConfirmationNotes = confirmationNotes ?? "Rejected by admin";
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Reject", "Order", id, $"Order {order.OrderNumber} rejected");
        TempData["Success"] = $"Order {order.OrderNumber} has been cancelled.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order is null) return NotFound();
        order.Status = status;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Update", "Order", id, $"Order {order.OrderNumber} marked as {status}");
        TempData["Success"] = $"Order {order.OrderNumber} marked as {status}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkUpdateStatus(int[] orderIds, OrderStatus status)
    {
        if (orderIds == null || orderIds.Length == 0)
        {
            TempData["Error"] = "No orders selected.";
            return RedirectToAction(nameof(Index));
        }

        var orders = await _context.Orders.Where(o => orderIds.Contains(o.Id)).ToListAsync();
        foreach (var order in orders)
        {
            order.Status = status;
        }
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("BulkUpdate", "Order", details: $"Bulk updated {orders.Count} orders to {status}");
        TempData["Success"] = $"{orders.Count} orders marked as {status}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Export(string? status)
    {
        var query = _context.Orders.AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var parsed))
            query = query.Where(o => o.Status == parsed);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Order Number,Customer Name,Customer Email,Phone,Total,Status,Created At");

        foreach (var o in orders)
        {
            csv.AppendLine($"{o.OrderNumber},\"{o.CustomerName}\",{o.CustomerEmail},{o.CustomerPhone},{o.Total:F2},{o.Status},{o.CreatedAt:yyyy-MM-dd HH:mm}");
        }

        return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"orders-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
