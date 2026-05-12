namespace ISPO.WebApp.ViewModels;

public class TeacherPanelViewModel
{
    public string UserFullName { get; set; } = string.Empty;
    public int StudentsCount { get; set; }
    public int CoursesCount { get; set; }
    public int GroupsCount { get; set; }
    public int LessonsCount { get; set; }
    public int LessonsThisWeek { get; set; }
    public int LiveLessonsCount { get; set; }
    public int PendingSubmissionsCount { get; set; }
    public LessonCalendarViewModel Calendar { get; set; } = new();
    public List<TeacherLessonVm> UpcomingLessons { get; set; } = new();
}

public class TeacherLessonVm
{
    public int ScheduleId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string GroupCode { get; set; } = string.Empty;
    public DateTime LessonDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? Room { get; set; }
    public string? Topic { get; set; }
    public string LiveStatus { get; set; } = "planned";
}
