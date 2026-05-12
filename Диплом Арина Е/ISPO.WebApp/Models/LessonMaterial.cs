namespace ISPO.WebApp.Models;

public class LessonMaterial
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public string CloudUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }

    public Schedule Schedule { get; set; } = null!;
}
