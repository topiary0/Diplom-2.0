namespace ISPO.WebApp.Models;

public class Schedule
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int GroupId { get; set; }
    public DateTime LessonDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? Room { get; set; }
    public string? LessonTopic { get; set; }
    public string? ConferenceUrl { get; set; }
    public string LiveStatus { get; set; } = "planned";
    public DateTime? LiveStartedAt { get; set; }
    public DateTime? LiveEndedAt { get; set; }

    public Course Course { get; set; } = null!;
    public StudentGroup Group { get; set; } = null!;
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<LessonMaterial> LessonMaterials { get; set; } = new List<LessonMaterial>();
    public ICollection<LessonSubmission> LessonSubmissions { get; set; } = new List<LessonSubmission>();
}
