namespace ISPO.WebApp.Models;

public class CourseMaterial
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public int SortOrder { get; set; }

    public Course Course { get; set; } = null!;
}
