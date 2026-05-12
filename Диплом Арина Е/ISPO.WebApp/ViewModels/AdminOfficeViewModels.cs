using System.ComponentModel.DataAnnotations;
using ISPO.WebApp.Infrastructure;
using ISPO.WebApp.Models;

namespace ISPO.WebApp.ViewModels;

public class AdminGroupsViewModel
{
    public List<StudentGroup> Groups { get; set; } = new();
    public CreateGroupViewModel Form { get; set; } = new();
}

public class CreateGroupViewModel
{
    [Required(ErrorMessage = "Укажите код группы.")]
    [StringLength(30)]
    public string GroupCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите название группы.")]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите специализацию.")]
    [StringLength(150)]
    public string Specialization { get; set; } = string.Empty;

    [Range(2000, 2100, ErrorMessage = "Год набора должен быть в диапазоне 2000-2100.")]
    public int StartYear { get; set; } = DateTime.Today.Year;
}

public class AdminTeachersViewModel
{
    public List<Teacher> Teachers { get; set; } = new();
    public CreateTeacherViewModel Form { get; set; } = new();
}

public class AdminCoursesViewModel
{
    public List<Teacher> Teachers { get; set; } = new();
    public List<CourseCategory> Categories { get; set; } = new();
    public List<AdminCourseListItemVm> Courses { get; set; } = new();
    public CreateCourseViewModel Form { get; set; } = new();
}

public class AdminCourseListItemVm
{
    public int Id { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationHours { get; set; }
    public bool IsActive { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public int EnrollmentsCount { get; set; }
    public int LessonsCount { get; set; }
}

public class CreateTeacherViewModel
{
    [Required(ErrorMessage = "Введите ФИО преподавателя.")]
    [StringLength(180)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите адрес электронной почты.")]
    [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты.")]
    [StringLength(120)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите пароль для входа.")]
    [StringLength(120, MinimumLength = 6, ErrorMessage = "Минимальная длина пароля — 6 символов.")]
    public string InitialPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите отделение.")]
    [StringLength(150)]
    public string Department { get; set; } = string.Empty;

    [StringLength(120)]
    public string? PositionName { get; set; }
}

public class AdminScheduleViewModel
{
    public List<ScheduleListItemVm> Items { get; set; } = new();
    public List<StudentGroup> Groups { get; set; } = new();
    public List<Course> Courses { get; set; } = new();
    public CreateScheduleViewModel Form { get; set; } = new();
}

public class ScheduleListItemVm
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int GroupId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string GroupCode { get; set; } = string.Empty;
    public DateTime LessonDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? Room { get; set; }
    public string? LessonTopic { get; set; }
    public string? ConferenceUrl { get; set; }
    public string LiveStatus { get; set; } = AppConstants.LessonLiveStatus.Planned;
}

public class CreateScheduleViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Выберите курс.")]
    public int CourseId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Выберите группу.")]
    public int GroupId { get; set; }

    [Required(ErrorMessage = "Укажите дату занятия.")]
    public DateTime LessonDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Укажите время начала.")]
    public TimeSpan StartTime { get; set; } = new(9, 0, 0);

    [Required(ErrorMessage = "Укажите время окончания.")]
    public TimeSpan EndTime { get; set; } = new(10, 30, 0);

    [StringLength(50)]
    public string? Room { get; set; }

    [StringLength(250)]
    public string? LessonTopic { get; set; }

    [StringLength(1000)]
    [RegularExpression(@"^https://.+", ErrorMessage = "Ссылка на конференцию должна начинаться с https://")]
    public string? ConferenceUrl { get; set; }
}

public class AdminEnrollmentsViewModel
{
    public List<EnrollmentListItemVm> Items { get; set; } = new();
    public List<Student> Students { get; set; } = new();
    public List<Course> Courses { get; set; } = new();
    public string? StatusFilter { get; set; }
    public CreateEnrollmentViewModel Form { get; set; } = new();
}

public class EnrollmentListItemVm
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string GroupCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateEnrollmentViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Выберите студента.")]
    public int StudentId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Выберите курс.")]
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Укажите статус.")]
    [RegularExpression("active|completed|cancelled", ErrorMessage = "Недопустимый статус.")]
    public string Status { get; set; } = "active";
}

public class ReportFilterVm
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? GroupId { get; set; }
    public int? CourseId { get; set; }
}

public class ReportSelectItemVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AdminReportViewModel
{
    public string ReportKey { get; set; } = string.Empty;
    public string ReportTitle { get; set; } = string.Empty;
    public ReportFilterVm Filter { get; set; } = new();
    public List<ReportSelectItemVm> Groups { get; set; } = new();
    public List<ReportSelectItemVm> Courses { get; set; } = new();
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
    public string EmptyMessage { get; set; } = "Нет данных для выбранных фильтров.";
}


