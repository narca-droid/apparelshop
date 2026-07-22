using ApparelShop.Models;
using Microsoft.AspNetCore.Mvc;

namespace ApparelShop.Controllers;

public class ErrorController : Controller
{
    [Route("/404")]
    public IActionResult NotFound404()
    {
        Response.StatusCode = 404;
        return View();
    }
}
