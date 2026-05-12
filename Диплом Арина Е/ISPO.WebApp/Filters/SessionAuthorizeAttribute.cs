using ISPO.WebApp.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ISPO.WebApp.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class SessionAuthorizeAttribute : Attribute, IAsyncActionFilter
{
    private readonly HashSet<string> _allowedRoles;

    public SessionAuthorizeAttribute(params string[] roles)
    {
        _allowedRoles = roles
            .Select(r => r.Trim().ToLowerInvariant())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .ToHashSet();
    }

    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32(AppSession.UserId);

        if (!userId.HasValue)
        {
            context.Result = new RedirectToActionResult("Login", "Auth", null);
            return Task.CompletedTask;
        }

        if (_allowedRoles.Count > 0)
        {
            var role = session.GetString(AppSession.UserRole)?.ToLowerInvariant();
            if (role is null || !_allowedRoles.Contains(role))
            {
                context.Result = new RedirectToActionResult("Forbidden", "Home", null);
                return Task.CompletedTask;
            }
        }

        return next();
    }
}
