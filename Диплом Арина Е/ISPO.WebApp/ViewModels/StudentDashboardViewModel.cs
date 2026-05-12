namespace ISPO.WebApp.ViewModels;

public class StudentDashboardViewModel
{
    public string UserFullName { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public int ActiveCoursesCount { get; set; }
    public decimal? AverageGrade { get; set; }
    public decimal AttendanceRate { get; set; }
    public bool IsDemoProfile { get; set; }
    public bool HasLinkedProfile { get; set; } = true;
    public LessonCalendarViewModel Calendar { get; set; } = new();
    public List<StudentCourseVm> EnrolledCourses { get; set; } = new();
    public List<StudentGradeVm> RecentGrades { get; set; } = new();
    public List<StudentLessonVm> UpcomingLessons { get; set; } = new();
    public List<StudentLiveLessonVm> LiveLessons { get; set; } = new();
    public List<StudentMaterialVm> LessonMaterials { get; set; } = new();
    public List<StudentSubmissionShortVm> LatestSubmissions { get; set; } = new();
}

public class StudentCourseVm
{
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public int DurationHours { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class StudentGradeVm
{
    public string CourseTitle { get; set; } = string.Empty;
    public decimal GradeValue { get; set; }
    public DateTime GradeDate { get; set; }
    public string? Comment { get; set; }
}

public class StudentLessonVm
{
    public int ScheduleId { get; set; }
    public DateTime LessonDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string? Room { get; set; }
    public string? Topic { get; set; }
    public string LiveStatus { get; set; } = "planned";
    public string? ConferenceUrl { get; set; }
}

public class StudentLiveLessonVm
{
    public int ScheduleId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public DateTime LessonDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? ConferenceUrl { get; set; }
}

public class StudentMaterialVm
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string MaterialTitle { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public string CloudUrl { get; set; } = string.Empty;
}

public class StudentSubmissionShortVm
{
    public int Id { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public DateTime LessonDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? Score { get; set; }
    public int RevisionNumber { get; set; }
    public DateTime SubmittedAt { get; set; }
}
