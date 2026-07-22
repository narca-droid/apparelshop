using Microsoft.AspNetCore.Mvc;

namespace ApparelShop.ViewComponents;

public class BreadcrumbsViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(List<BreadcrumbItem> items)
    {
        return View(items);
    }
}

public class BreadcrumbItem
{
    public string Text { get; set; } = string.Empty;
    public string? Url { get; set; }
}
