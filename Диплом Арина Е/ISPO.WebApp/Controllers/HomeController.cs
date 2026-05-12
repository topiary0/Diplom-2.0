using ISPO.WebApp.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ISPO.WebApp.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (HttpContext.Session.GetInt32(AppSession.UserId).HasValue)
        {
            var role = HttpContext.Session.GetString(AppSession.UserRole)?.ToLowerInvariant();
            return role switch
            {
                "admin" => RedirectToAction("Index", "Admin"),
                "teacher" => RedirectToAction("Index", "Teacher"),
                "student" => RedirectToAction("Index", "Student"),
                _ => RedirectToAction("Login", "Auth")
            };
        }

        return RedirectToAction("Login", "Auth");
    }

    public IActionResult Forbidden()
    {
        return View();
    }

    public IActionResult Error()
    {
        return View();
    }
}
