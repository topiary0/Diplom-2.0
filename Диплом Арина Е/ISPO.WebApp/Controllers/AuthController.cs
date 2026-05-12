using ISPO.WebApp.Data;
using ISPO.WebApp.Infrastructure;
using ISPO.WebApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISPO.WebApp.Controllers;

public class AuthController : Controller
{
    private readonly DiplomIspoDbContext _db;

    public AuthController(DiplomIspoDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetInt32(AppSession.UserId).HasValue)
            return RedirectToDashboard(HttpContext.Session.GetString(AppSession.UserRole));

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var email = NormalizeEmail(model.Email);
        var password = model.Password.Trim();

        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.IsActive &&
            u.Email != null &&
            u.Email.Trim().ToLower() == email);

        if (user is null || !string.Equals((user.PasswordHash ?? string.Empty).Trim(), password, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
            return View(model);
        }

        HttpContext.Session.SetInt32(AppSession.UserId, user.Id);
        HttpContext.Session.SetString(AppSession.UserName, user.FullName.Trim());
        HttpContext.Session.SetString(AppSession.UserRole, user.Role.Trim().ToLowerInvariant());
        HttpContext.Session.SetString(AppSession.UserEmail, email);

        return RedirectToDashboard(user.Role);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [ActionName("Logout")]
    public IActionResult LogoutGet()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    private IActionResult RedirectToDashboard(string? role)
    {
        return role?.ToLowerInvariant() switch
        {
            "admin" => RedirectToAction("Index", "Admin"),
            "teacher" => RedirectToAction("Index", "Teacher"),
            "student" => RedirectToAction("Index", "Student"),
            _ => RedirectToAction(nameof(Login))
        };
    }

    private static string NormalizeEmail(string? email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();
}
