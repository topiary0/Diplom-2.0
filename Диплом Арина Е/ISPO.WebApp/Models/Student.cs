namespace ISPO.WebApp.Models;

public class Student
{
    public int Id { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public DateTime BirthDate { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int GroupId { get; set; }
    public DateTime CreatedAt { get; set; }

    public StudentGroup Group { get; set; } = null!;
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<LessonSubmission> LessonSubmissions { get; set; } = new List<LessonSubmission>();
}
