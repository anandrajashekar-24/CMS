using System.Diagnostics;
using System.Net;
using CMS.Models;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    // Simple server-side action one of the demo buttons posts to.
    // This is where you'd eventually forward the call to the REST API instead
    // of just returning a canned response.
    [HttpPost]
    public IActionResult Ping()
    {
        _logger.LogInformation("Ping button was clicked at {Time}", DateTime.UtcNow);
        return Json(new { message = "Pong from the server!", timestampUtc = DateTime.UtcNow });
    }

    [HttpGet]
    public IActionResult NodeIp()
    {
        var localIp = HttpContext.Connection.LocalIpAddress?.ToString() ?? "unknown";

        if (IPAddress.TryParse(localIp, out var parsedLocalIp) && parsedLocalIp.Equals(IPAddress.IPv6Loopback))
        {
            localIp = IPAddress.Loopback.ToString();
        }

        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        var publicIp = forwardedFor?.Split(',')[0].Trim();

        if (string.IsNullOrWhiteSpace(publicIp))
        {
            publicIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        if (IPAddress.TryParse(publicIp, out var parsedPublicIp) && parsedPublicIp.Equals(IPAddress.IPv6Loopback))
        {
            publicIp = IPAddress.Loopback.ToString();
        }

        _logger.LogInformation(
            "Node IP button was clicked. Local IP: {LocalIp}, Public IP: {PublicIp}",
            localIp,
            publicIp);

        return Json(new { localIp, publicIp });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
