using ApparelShop.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApparelShop.Areas.Admin.Controllers;

public abstract class AdminBaseController : Controller
{
    private readonly IWebHostEnvironment _env;

    protected AdminBaseController(IWebHostEnvironment env)
    {
        _env = env;
    }

    protected async Task<string> SaveUploadAsync(IFormFile file)
    {
        var (isValid, error) = FileUploadValidator.Validate(file);
        if (!isValid)
            throw new InvalidOperationException(error);

        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsFolder);
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsFolder, fileName);
        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);
        return $"/uploads/{fileName}";
    }
}
