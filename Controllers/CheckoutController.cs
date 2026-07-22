using System.Security.Cryptography;
using System.Text.Json;
using ApparelShop.Data;
using ApparelShop.Models;
using ApparelShop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ApparelShop.Controllers;

public class CheckoutController : Controller
{
    private const string SessionKey = "CartV1";
    private const string CouponSessionKey = "AppliedCoupon";
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailService emailService, ILogger<CheckoutController> logger)
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
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

    public IActionResult Index()
    {
        var cart = GetCart();
        if (cart.Items.Count == 0)
        {
            TempData["Error"] = "Your bag is empty.";
            return RedirectToAction("Index", "Cart");
        }

        var couponJson = HttpContext.Session.GetString(CouponSessionKey);
        ViewBag.AppliedCoupon = string.IsNullOrEmpty(couponJson) ? null : JsonSerializer.Deserialize<Coupon>(couponJson);

        return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyCoupon(string couponCode)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        var cart = GetCart();
        if (cart.Items.Count == 0)
        {
            if (isAjax) return Json(new { success = false, message = "Your bag is empty." });
            TempData["Error"] = "Your bag is empty.";
            return RedirectToAction("Index", "Cart");
        }

        if (string.IsNullOrWhiteSpace(couponCode))
        {
            if (isAjax) return Json(new { success = false, message = "Please enter a coupon code." });
            TempData["Error"] = "Please enter a coupon code.";
            return RedirectToAction("Index");
        }

        var coupon = await _context.Coupons
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code.ToLower() == couponCode.ToLower() && c.IsUsable);

        if (coupon is null)
        {
            if (isAjax) return Json(new { success = false, message = "Invalid or expired coupon code." });
            TempData["Error"] = "Invalid or expired coupon code.";
            return RedirectToAction("Index");
        }

        if (coupon.MinOrderAmount.HasValue && cart.Subtotal < coupon.MinOrderAmount.Value)
        {
            var msg = $"Minimum order of AED {coupon.MinOrderAmount.Value:0.00} required for this coupon.";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["Error"] = msg;
            return RedirectToAction("Index");
        }

        HttpContext.Session.SetString(CouponSessionKey, JsonSerializer.Serialize(coupon));
        if (isAjax) return Json(new { success = true, message = $"Coupon '{coupon.Code}' applied!" });
        TempData["Success"] = $"Coupon '{coupon.Code}' applied successfully!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveCoupon()
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        HttpContext.Session.Remove(CouponSessionKey);
        if (isAjax) return Json(new { success = true, message = "Coupon removed." });
        TempData["Success"] = "Coupon removed.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(string customerName, string customerEmail, string customerPhone,
        string shippingAddress, string city, string emirate, string? notes)
    {
        var cart = GetCart();
        if (cart.Items.Count == 0)
        {
            TempData["Error"] = "Your bag is empty.";
            return RedirectToAction("Index", "Cart");
        }

        if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(customerEmail) ||
            string.IsNullOrWhiteSpace(customerPhone) || string.IsNullOrWhiteSpace(shippingAddress))
        {
            TempData["Error"] = "Please fill in all required fields.";
            return RedirectToAction("Index");
        }

        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var staleItems = cart.Items.Where(i => !products.ContainsKey(i.ProductId)).ToList();
        if (staleItems.Count > 0)
        {
            foreach (var item in staleItems) cart.Items.Remove(item);
            SaveCart(cart);
        }

        if (cart.Items.Count == 0)
        {
            TempData["Error"] = "Your bag is empty. The products you added are no longer available.";
            return RedirectToAction("Index", "Cart");
        }

        foreach (var item in cart.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                TempData["Error"] = $"Product '{item.ProductName}' is no longer available.";
                return RedirectToAction("Index", "Cart");
            }

            if (!product.IsActive || product.IsDeleted)
            {
                TempData["Error"] = $"Product '{item.ProductName}' is no longer available.";
                return RedirectToAction("Index", "Cart");
            }

            if (product.StockQuantity < item.Quantity)
            {
                TempData["Error"] = $"Insufficient stock for '{item.ProductName}'. Only {product.StockQuantity} available.";
                return RedirectToAction("Index", "Cart");
            }

            item.UnitPrice = product.Price;
        }

        const decimal shippingFee = 25.00m;
        decimal discount = 0;
        var couponJson = HttpContext.Session.GetString(CouponSessionKey);
        var appliedCoupon = string.IsNullOrEmpty(couponJson) ? null : JsonSerializer.Deserialize<Coupon>(couponJson);

        if (appliedCoupon is not null && appliedCoupon.IsUsable)
        {
            discount = appliedCoupon.Type == CouponType.Percentage
                ? Math.Round(cart.Subtotal * appliedCoupon.Value / 100, 2)
                : appliedCoupon.Value;

            if (appliedCoupon.MaxDiscountAmount.HasValue && discount > appliedCoupon.MaxDiscountAmount.Value)
                discount = appliedCoupon.MaxDiscountAmount.Value;

            if (discount > cart.Subtotal) discount = cart.Subtotal;
        }

        var order = new Order
        {
            OrderNumber = "AURA-" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(8)).Replace("+", "-").Replace("/", "_").TrimEnd('='),
            UserId = _userManager.GetUserId(User),
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            CustomerPhone = customerPhone,
            ShippingAddress = shippingAddress,
            City = city,
            Emirate = emirate,
            Notes = notes,
            Subtotal = cart.Subtotal,
            ShippingFee = shippingFee,
            Total = cart.Subtotal + shippingFee - discount,
            Status = OrderStatus.Pending
        };

        foreach (var item in cart.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Size = item.Size,
                Color = item.Color,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity
            });

            if (products.TryGetValue(item.ProductId, out var p))
            {
                p.StockQuantity -= item.Quantity;
            }
        }

        if (appliedCoupon is not null && appliedCoupon.IsUsable)
        {
            var couponEntity = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == appliedCoupon.Id);
            if (couponEntity is not null)
            {
                couponEntity.TimesUsed++;
            }
        }

        _context.Orders.Add(order);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to save order for user {UserId}.", _userManager.GetUserId(User));

            TempData["Error"] = "An error occurred placing your order. Your cart has been cleared. Please try again.";
            HttpContext.Session.Remove(SessionKey);
            HttpContext.Session.Remove(CouponSessionKey);
            return RedirectToAction("Index", "Cart");
        }

        HttpContext.Session.Remove(SessionKey);
        HttpContext.Session.Remove(CouponSessionKey);

        _ = _emailService.SendOrderConfirmationAsync(order);

        return RedirectToAction("Confirmation", new { orderNumber = order.OrderNumber });
    }

    public async Task<IActionResult> Confirmation(string orderNumber)
    {
        if (string.IsNullOrEmpty(orderNumber)) return NotFound();

        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

        if (order is null) return NotFound();

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _userManager.GetUserId(User);
            if (order.UserId != null && order.UserId != userId)
                return Forbid();
        }

        return View(order);
    }

    public IActionResult Tracking()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Tracking(string orderNumber, string email)
    {
        if (string.IsNullOrWhiteSpace(orderNumber) || string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Please enter both order number and email.";
            return RedirectToAction("Tracking");
        }

        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber && o.CustomerEmail == email);

        if (order is null)
        {
            TempData["Error"] = "Order not found. Please check your order number and email address.";
            return RedirectToAction("Tracking");
        }

        return View("TrackingResult", order);
    }
}
