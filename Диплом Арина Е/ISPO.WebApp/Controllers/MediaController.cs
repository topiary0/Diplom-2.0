using ISPO.WebApp.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace ISPO.WebApp.Controllers;

[SessionAuthorize("admin", "teacher", "student")]
[Route("Media")]
public class MediaController : Controller
{
    private readonly IWebHostEnvironment _environment;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    public MediaController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet("News/{fileName}")]
    public IActionResult News(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains('/') || fileName.Contains('\\'))
            return NotFound();

        var root = GetNewsStorageDirectory();
        var fullPath = Path.Combine(root, fileName);
        if (!System.IO.File.Exists(fullPath))
            return NotFound();

        if (!_contentTypeProvider.TryGetContentType(fileName, out var contentType))
            contentType = "application/octet-stream";

        return PhysicalFile(fullPath, contentType);
    }

    private string GetNewsStorageDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
            localAppData = _environment.ContentRootPath;

        return Path.Combine(localAppData, "ISPO.WebApp", "uploads", "news");
    }
}
