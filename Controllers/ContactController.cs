using ApparelShop.Data;
using ApparelShop.Models;
using Microsoft.AspNetCore.Mvc;

namespace ApparelShop.Controllers;

public class ContactController : Controller
{
    private readonly ApplicationDbContext _context;

    public ContactController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string name, string email, string subject, string message)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "Please fill in all fields.";
            return RedirectToAction("Index");
        }

        _context.ContactMessages.Add(new ContactMessage
        {
            Name = name,
            Email = email,
            Subject = subject,
            Message = message
        });
        await _context.SaveChangesAsync();

        TempData["Success"] = "Your message has been sent. We'll get back to you soon!";
        return RedirectToAction("Index");
    }
}
