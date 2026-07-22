using ApparelShop.Data;
using ApparelShop.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApparelShop.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
    {
        var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is not null && await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Account locked out. Please try again later.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        }
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string fullName, string email, string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            ModelState.AddModelError(string.Empty, "Passwords do not match.");
            return View();
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Customer");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token }, protocol: Request.Scheme);

            // In production, send this link via email:
            // await _emailService.SendEmailConfirmationAsync(user.Email, confirmLink);

            // For demo purposes, show the link
            ViewBag.ConfirmLink = confirmLink;
            ViewBag.Message = "Please check your email to confirm your account.";
            return View("RegisterConfirmation");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            TempData["Error"] = "Invalid email confirmation link.";
            return RedirectToAction("Index", "Home");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            TempData["Error"] = "Invalid email confirmation link.";
            return RedirectToAction("Index", "Home");
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded)
        {
            TempData["Success"] = "Your email has been confirmed. You can now sign in.";
            return RedirectToAction("Login");
        }

        TempData["Error"] = "Email confirmation failed. Please try again.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        if (!User.Identity?.IsAuthenticated ?? true) return RedirectToAction("Login");

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login");

        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.UserId == user.Id)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        ViewBag.User = user;
        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        if (!User.Identity?.IsAuthenticated ?? true) return RedirectToAction("Login");

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login");

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(string fullName, string email)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login");

        user.FullName = fullName;
        user.Email = email;
        user.UserName = email;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction("Profile");
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action("ResetPassword", "Account", new { email, token }, protocol: Request.Scheme);

            // In production, send this link via email:
            // await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);

            // For demo purposes, show the link on the confirmation page
            ViewBag.ResetLink = resetLink;
        }

        // Always show same message to prevent email enumeration
        ViewBag.Message = "If an account with that email exists, a password reset link has been sent.";
        return View("ForgotPasswordConfirmation");
    }

    [HttpGet]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        {
            TempData["Error"] = "Invalid password reset link.";
            return RedirectToAction("ForgotPassword");
        }
        ViewBag.Email = email;
        ViewBag.Token = token;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string email, string token, string newPassword, string confirmPassword)
    {
        if (newPassword != confirmPassword)
        {
            ModelState.AddModelError(string.Empty, "Passwords do not match.");
            ViewBag.Email = email;
            ViewBag.Token = token;
            return View();
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            TempData["Success"] = "Your password has been reset. Please sign in.";
            return RedirectToAction("Login");
        }

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (result.Succeeded)
        {
            TempData["Success"] = "Your password has been reset. Please sign in.";
            return RedirectToAction("Login");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        ViewBag.Email = email;
        ViewBag.Token = token;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> OrderDetails(string orderNumber)
    {
        if (!User.Identity?.IsAuthenticated ?? true) return RedirectToAction("Login");

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction("Login");

        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber && o.UserId == user.Id);

        if (order is null) return NotFound();
        return View(order);
    }
}
