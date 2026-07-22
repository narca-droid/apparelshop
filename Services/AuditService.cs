using ApparelShop.Data;
using ApparelShop.Models;
using Microsoft.AspNetCore.Identity;

namespace ApparelShop.Services;

public class AuditService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string entity, int? entityId = null, string? details = null)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        var user = principal is not null ? _userManager.GetUserId(principal) : null;
        var userName = principal is not null ? _userManager.GetUserName(principal) : null;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = user,
            UserName = userName,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Details = details
        });

        await _context.SaveChangesAsync();
    }
}
