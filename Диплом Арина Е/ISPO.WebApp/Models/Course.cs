namespace ISPO.WebApp.Models;

public class Course
{
    public int Id { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationHours { get; set; }
    public int CategoryId { get; set; }
    public int TeacherId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public CourseCategory Category { get; set; } = null!;
    public Teacher Teacher { get; set; } = null!;
    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<CourseMaterial> Materials { get; set; } = new List<CourseMaterial>();
}
