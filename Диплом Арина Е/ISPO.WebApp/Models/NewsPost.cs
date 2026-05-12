namespace ISPO.WebApp.Models;

public class NewsPost
{
    public int Id { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
