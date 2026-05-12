namespace ISPO.WebApp.Models;

public class LessonSubmission
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public int StudentId { get; set; }
    public string CloudFileUrl { get; set; } = string.Empty;
    public string? StudentComment { get; set; }
    public string Status { get; set; } = "new";
    public decimal? Score { get; set; }
    public string? TeacherComment { get; set; }
    public int RevisionNumber { get; set; } = 1;
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public Schedule Schedule { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
