using ISPO.WebApp.Filters;
using ISPO.WebApp.Infrastructure;
using ISPO.WebApp.Models;
using ISPO.WebApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ISPO.WebApp.Controllers;

[SessionAuthorize("admin", "teacher", "student")]
public class SupportController : Controller
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SupportController> _logger;
    private static readonly SemaphoreSlim StorageLock = new(1, 1);

    public SupportController(IWebHostEnvironment environment, ILogger<SupportController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    [SessionAuthorize("teacher", "student")]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await BuildSupportCenterViewModelAsync());
    }

    [SessionAuthorize("teacher", "student")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "Form")] CreateSupportRequestForm? form)
    {
        form ??= new CreateSupportRequestForm();
        form.Subject = (form.Subject ?? string.Empty).Trim();
        form.Message = (form.Message ?? string.Empty).Trim();

        if (!ModelState.IsValid)
            return View("Index", await BuildSupportCenterViewModelAsync(form));

        var userId = HttpContext.Session.GetInt32(AppSession.UserId);
        if (!userId.HasValue)
            return RedirectToAction("Login", "Auth");

        var role = (HttpContext.Session.GetString(AppSession.UserRole) ?? string.Empty).Trim().ToLowerInvariant();
        var fullName = (HttpContext.Session.GetString(AppSession.UserName) ?? "Пользователь").Trim();
        var email = (HttpContext.Session.GetString(AppSession.UserEmail) ?? string.Empty).Trim().ToLowerInvariant();

        var list = await LoadRequestsAsync();
        var nextId = list.Count == 0 ? 1 : list.Max(x => x.Id) + 1;

        list.Add(new SupportRequest
        {
            Id = nextId,
            SenderUserId = userId.Value,
            SenderName = fullName,
            SenderEmail = email,
            SenderRole = role,
            Subject = form.Subject,
            Message = form.Message,
            Status = "new",
            CreatedAt = DateTime.UtcNow
        });

        await SaveRequestsAsync(list);
        TempData["Success"] = "Обращение отправлено администратору.";
        return RedirectToAction(nameof(Index));
    }

    [SessionAuthorize("admin")]
    [HttpGet]
    public async Task<IActionResult> Admin(string? status = null)
    {
        var normalizedStatus = NormalizeStatusFilter(status);
        var allRequests = await LoadRequestsAsync();

        var filtered = string.IsNullOrWhiteSpace(normalizedStatus)
            ? allRequests
            : allRequests.Where(x => x.Status == normalizedStatus).ToList();

        var vm = new AdminSupportRequestsViewModel
        {
            StatusFilter = normalizedStatus,
            TotalCount = allRequests.Count,
            NewCount = allRequests.Count(x => x.Status == "new"),
            InProgressCount = allRequests.Count(x => x.Status == "in_progress"),
            ClosedCount = allRequests.Count(x => x.Status == "closed"),
            Items = filtered
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new SupportRequestItemVm
                {
                    Id = x.Id,
                    SenderName = x.SenderName,
                    SenderEmail = x.SenderEmail,
                    SenderRole = x.SenderRole,
                    Subject = x.Subject,
                    Message = x.Message,
                    Status = x.Status,
                    AdminComment = x.AdminComment,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToList()
        };

        ViewData["Title"] = "Обращения в поддержку";
        return View(vm);
    }

    [SessionAuthorize("admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string status, string? adminComment, string? statusFilter)
    {
        var list = await LoadRequestsAsync();
        var ticket = list.FirstOrDefault(x => x.Id == id);

        if (ticket is null)
        {
            TempData["Error"] = "Обращение не найдено.";
            return RedirectToAction(nameof(Admin), new { status = NormalizeStatusFilter(statusFilter) });
        }

        ticket.Status = NormalizeStatus(status);
        ticket.AdminComment = string.IsNullOrWhiteSpace(adminComment) ? null : adminComment.Trim();
        ticket.UpdatedAt = DateTime.UtcNow;
        ticket.ProcessedAt = ticket.Status == "closed" ? DateTime.UtcNow : null;

        await SaveRequestsAsync(list);
        TempData["Success"] = "Обращение обновлено.";
        return RedirectToAction(nameof(Admin), new { status = NormalizeStatusFilter(statusFilter) });
    }

    private async Task<SupportCenterViewModel> BuildSupportCenterViewModelAsync(CreateSupportRequestForm? form = null)
    {
        var userId = HttpContext.Session.GetInt32(AppSession.UserId);
        if (!userId.HasValue)
            return new SupportCenterViewModel();

        var role = (HttpContext.Session.GetString(AppSession.UserRole) ?? string.Empty).Trim().ToLowerInvariant();
        var allRequests = await LoadRequestsAsync();

        return new SupportCenterViewModel
        {
            RoleTitle = role == "teacher" ? "преподавателя" : "ученика",
            Form = form ?? new CreateSupportRequestForm(),
            Items = allRequests
                .Where(x => x.SenderUserId == userId.Value)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new SupportRequestItemVm
                {
                    Id = x.Id,
                    SenderName = x.SenderName,
                    SenderEmail = x.SenderEmail,
                    SenderRole = x.SenderRole,
                    Subject = x.Subject,
                    Message = x.Message,
                    Status = x.Status,
                    AdminComment = x.AdminComment,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToList()
        };
    }

    private async Task<List<SupportRequest>> LoadRequestsAsync()
    {
        try
        {
            var storageFile = GetStorageFilePath();
            if (!System.IO.File.Exists(storageFile))
                return [];

            await using var stream = new FileStream(storageFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true);
            var data = await JsonSerializer.DeserializeAsync<List<SupportRequest>>(stream);
            return data ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка чтения файла обращений поддержки.");
            return [];
        }
    }

    private async Task SaveRequestsAsync(List<SupportRequest> requests)
    {
        var storageFile = GetStorageFilePath();
        var storageDir = Path.GetDirectoryName(storageFile);
        if (!string.IsNullOrWhiteSpace(storageDir))
            Directory.CreateDirectory(storageDir);

        await StorageLock.WaitAsync();
        try
        {
            await using var stream = new FileStream(storageFile, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, useAsync: true);
            await JsonSerializer.SerializeAsync(stream, requests, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await stream.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сохранения файла обращений поддержки.");
            TempData["Error"] = "Не удалось сохранить обращение. Попробуйте еще раз.";
        }
        finally
        {
            StorageLock.Release();
        }
    }

    private string GetStorageFilePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
            localAppData = _environment.ContentRootPath;

        return Path.Combine(localAppData, "ISPO.WebApp", "support", "support_requests.json");
    }

    private static string NormalizeStatus(string? status)
    {
        var value = (status ?? string.Empty).Trim().ToLowerInvariant();
        return value switch
        {
            "in_progress" => "in_progress",
            "closed" => "closed",
            _ => "new"
        };
    }

    private static string? NormalizeStatusFilter(string? status)
    {
        var value = (status ?? string.Empty).Trim().ToLowerInvariant();
        return value switch
        {
            "new" => "new",
            "in_progress" => "in_progress",
            "closed" => "closed",
            _ => null
        };
    }
}
