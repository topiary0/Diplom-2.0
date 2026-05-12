namespace ISPO.WebApp.ViewModels;

public class AdminDashboardViewModel
{
    public string UserFullName { get; set; } = string.Empty;
    public int UsersCount { get; set; }
    public int ActiveUsersCount { get; set; }
    public int StudentsCount { get; set; }
    public int TeachersCount { get; set; }
    public int GroupsCount { get; set; }
    public int CoursesCount { get; set; }
    public int LessonsThisWeek { get; set; }
    public int LiveLessonsCount { get; set; }
    public int SubmissionsPendingCount { get; set; }
    public List<CourseLoadVm> TopCourses { get; set; } = new();
    public List<NewsCardVm> News { get; set; } = new();
    public CreateNewsViewModel NewsForm { get; set; } = new();
}

public class CourseLoadVm
{
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public int StudentsEnrolled { get; set; }
}
