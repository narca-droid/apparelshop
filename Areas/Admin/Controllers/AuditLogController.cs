using ApparelShop.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApparelShop.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AuditLogController : Controller
{
    private readonly ApplicationDbContext _context;

    public AuditLogController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        const int pageSize = 25;
        var query = _context.AuditLogs.AsQueryable();

        var total = await query.CountAsync();
        var logs = await query.OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

        return View(logs);
    }
}
