namespace ISPO.WebApp.ViewModels;

public class LessonCalendarViewModel
{
    public string ViewMode { get; set; } = "week";
    public DateTime AnchorDate { get; set; } = DateTime.Today;
    public DateTime RangeStart { get; set; } = DateTime.Today;
    public DateTime RangeEndInclusive { get; set; } = DateTime.Today;
    public string RangeLabel { get; set; } = string.Empty;
    public int? SelectedCourseId { get; set; }
    public int? SelectedGroupId { get; set; }
    public List<CalendarFilterOptionVm> Courses { get; set; } = new();
    public List<CalendarFilterOptionVm> Groups { get; set; } = new();
    public List<CalendarDayVm> Days { get; set; } = new();
}

public class CalendarFilterOptionVm
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class CalendarDayVm
{
    public DateTime Date { get; set; }
    public bool IsToday { get; set; }
    public bool IsCurrentPeriod { get; set; } = true;
    public List<CalendarLessonVm> Lessons { get; set; } = new();
}

public class CalendarLessonVm
{
    public int ScheduleId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string GroupCode { get; set; } = string.Empty;
    public string? Topic { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string LiveStatus { get; set; } = "planned";
}
