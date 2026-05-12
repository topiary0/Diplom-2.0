namespace ISPO.WebApp.Models;

public class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public DateTime EnrolledAt { get; set; }
    public string Status { get; set; } = string.Empty;

    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
