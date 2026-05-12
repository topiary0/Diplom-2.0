namespace ISPO.WebApp.Models;

public class Attendance
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public int StudentId { get; set; }
    public bool IsPresent { get; set; }
    public DateTime MarkedAt { get; set; }

    public Schedule Schedule { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
