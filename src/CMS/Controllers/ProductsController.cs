using Microsoft.AspNetCore.Mvc;

namespace CMS.Controllers;

public class ProductsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Projects() => Stub("Projects");

    public IActionResult Wip() => Stub("WIP");

    public IActionResult Materials() => Stub("Materials");

    public IActionResult Reports() => Stub("Reports");

    private IActionResult Stub(string section)
    {
        ViewData["Title"] = section;
        return View("Stub", section);
    }
}
