using ISPO.WebApp.Data;
using ISPO.WebApp.Filters;
using ISPO.WebApp.Infrastructure;
using ISPO.WebApp.Models;
using ISPO.WebApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISPO.WebApp.Controllers;

[SessionAuthorize("admin", "teacher", "student")]
public class NewsController : Controller
{
    private readonly DiplomIspoDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<NewsController> _logger;
    private static readonly string[] AllowedNewsImageExtensions = [".jpg", ".jpeg", ".png", ".jfif", ".webp"];
    private const long MaxNewsImageSizeBytes = 25L * 1024 * 1024;

    public NewsController(DiplomIspoDbContext db, IWebHostEnvironment environment, ILogger<NewsController> logger)
    {
        _db = db;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await BuildNewsPageViewModelAsync());
    }

    [HttpPost]
    [SessionAuthorize("admin")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(200L * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 200L * 1024 * 1024)]
    public async Task<IActionResult> Create([Bind(Prefix = "NewsForm")] CreateNewsViewModel? form, IFormFile? newsImage)
    {
        form ??= new CreateNewsViewModel();
        form.Body = (form.Body ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(form.Body))
            ModelState.AddModelError("NewsForm.Body", "Введите текст новости.");

        if (!ModelState.IsValid)
            return View("Index", await BuildNewsPageViewModelAsync(form));

        try
        {
            string? imageUrl = null;
            if (newsImage is not null && newsImage.Length > 0)
            {
                if (newsImage.Length > MaxNewsImageSizeBytes)
                    ModelState.AddModelError("NewsImage", "Размер изображения не должен превышать 25 МБ.");

                var fileNameExtension = (Path.GetExtension(newsImage.FileName) ?? string.Empty).Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(fileNameExtension) && !AllowedNewsImageExtensions.Contains(fileNameExtension))
                    ModelState.AddModelError("NewsImage", "Поддерживаются изображения JPG, PNG и WEBP.");

                var detectedExtension = await DetectNewsImageExtensionAsync(newsImage);
                if (string.IsNullOrWhiteSpace(detectedExtension))
                    ModelState.AddModelError("NewsImage", "Файл не распознан как изображение JPG, PNG или WEBP.");

                if (!ModelState.IsValid)
                    return View("Index", await BuildNewsPageViewModelAsync(form));

                try
                {
                    imageUrl = await SaveNewsImageAsync(newsImage, detectedExtension!);
                }
                catch (Exception ex)
                {
                    imageUrl = null;
                    _logger.LogError(ex, "Не удалось сохранить изображение новости. Новость будет опубликована без фото.");
                    TempData["Error"] = "Новость опубликована без изображения: не удалось сохранить файл.";
                }
            }

            _db.NewsPosts.Add(new NewsPost
            {
                Body = form.Body,
                ImageUrl = imageUrl,
                CreatedBy = HttpContext.Session.GetString(AppSession.UserName) ?? "Администратор",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = "Новость опубликована.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка публикации новости. Пользователь: {UserId}", HttpContext.Session.GetInt32(AppSession.UserId));
            TempData["Error"] = "Не удалось опубликовать новость. Попробуйте снова.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [SessionAuthorize("admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var news = await _db.NewsPosts.FindAsync(id);
            if (news is null)
                return RedirectToAction(nameof(Index));

            if (!string.IsNullOrWhiteSpace(news.ImageUrl))
            {
                var fullPath = ResolveNewsImageFullPath(news.ImageUrl);
                if (!string.IsNullOrWhiteSpace(fullPath) && System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }

            _db.NewsPosts.Remove(news);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Новость удалена.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка удаления новости Id={NewsId}", id);
            TempData["Error"] = "Не удалось удалить новость.";
            return RedirectToAction(nameof(Index));
        }
    }

    private async Task<NewsPageViewModel> BuildNewsPageViewModelAsync(CreateNewsViewModel? form = null)
    {
        List<NewsCardVm> news;
        try
        {
            news = await _db.NewsPosts
                .AsNoTracking()
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NewsCardVm
                {
                    Id = n.Id,
                    Body = n.Body,
                    ImageUrl = n.ImageUrl,
                    CreatedBy = n.CreatedBy,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка чтения новостей.");
            TempData["Error"] ??= "Новости временно недоступны.";
            news = [];
        }

        var role = HttpContext.Session.GetString(AppSession.UserRole)?.Trim().ToLowerInvariant();
        return new NewsPageViewModel
        {
            UserFullName = HttpContext.Session.GetString(AppSession.UserName) ?? "Пользователь",
            CanManageNews = role == "admin",
            News = news,
            NewsForm = form ?? new CreateNewsViewModel()
        };
    }

    private async Task<string> SaveNewsImageAsync(IFormFile file, string extension)
    {
        var uploadsDir = GetNewsStorageDirectory();
        Directory.CreateDirectory(uploadsDir);

        if (string.IsNullOrWhiteSpace(extension))
            throw new InvalidOperationException("Файл не распознан как изображение JPG, PNG или WEBP.");

        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsDir, fileName);
        var tempPath = Path.Combine(uploadsDir, $"{Guid.NewGuid():N}.tmp");

        try
        {
            await using var source = file.OpenReadStream();
            await using (var destination = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 64, useAsync: true))
            {
                await source.CopyToAsync(destination);
                await destination.FlushAsync();
            }

            System.IO.File.Move(tempPath, fullPath);
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
        }

        return $"/Media/News/{fileName}";
    }

    private async Task<string?> DetectNewsImageExtensionAsync(IFormFile file)
    {
        if (file.Length <= 0)
            return null;

        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
        if (read < 3)
            return null;

        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return ".jpg";

        if (read >= 8 &&
            header[0] == 0x89 &&
            header[1] == 0x50 &&
            header[2] == 0x4E &&
            header[3] == 0x47 &&
            header[4] == 0x0D &&
            header[5] == 0x0A &&
            header[6] == 0x1A &&
            header[7] == 0x0A)
            return ".png";

        if (read >= 12 &&
            header[0] == 0x52 &&
            header[1] == 0x49 &&
            header[2] == 0x46 &&
            header[3] == 0x46 &&
            header[8] == 0x57 &&
            header[9] == 0x45 &&
            header[10] == 0x42 &&
            header[11] == 0x50)
            return ".webp";

        return null;
    }

    private string GetNewsStorageDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
            localAppData = _environment.ContentRootPath;

        return Path.Combine(localAppData, "ISPO.WebApp", "uploads", "news");
    }

    private string? ResolveNewsImageFullPath(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return null;

        var normalized = imageUrl.Trim();
        if (normalized.StartsWith("/Media/News/", StringComparison.OrdinalIgnoreCase))
        {
            var fileName = normalized["/Media/News/".Length..];
            if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains('/') || fileName.Contains('\\'))
                return null;

            return Path.Combine(GetNewsStorageDirectory(), fileName);
        }

        if (normalized.StartsWith("/uploads/news/", StringComparison.OrdinalIgnoreCase))
        {
            var webRootPath = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
                webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");

            var relativePath = normalized.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(webRootPath, relativePath);
        }

        return null;
    }
}
