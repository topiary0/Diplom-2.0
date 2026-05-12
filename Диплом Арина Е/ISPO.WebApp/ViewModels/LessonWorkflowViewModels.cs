using System.ComponentModel.DataAnnotations;
using ISPO.WebApp.Infrastructure;

namespace ISPO.WebApp.ViewModels;

public class TeacherLessonsViewModel
{
    public List<TeacherLessonCardVm> Lessons { get; set; } = new();
    public List<TeacherLessonCourseOptionVm> Courses { get; set; } = new();
    public List<TeacherLessonGroupOptionVm> Groups { get; set; } = new();
    public CreateTeacherLessonViewModel Form { get; set; } = new();
}

public class TeacherLessonCourseOptionVm
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class TeacherLessonGroupOptionVm
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class CreateTeacherLessonViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Выберите курс.")]
    public int CourseId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Выберите группу.")]
    public int GroupId { get; set; }

    [Required(ErrorMessage = "Укажите дату урока.")]
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
    [RegularExpression(@"^https://.+", ErrorMessage = "Ссылка должна начинаться с https://")]
    public string? ConferenceUrl { get; set; }
}

public class TeacherEditLessonViewModel
{
    public int ScheduleId { get; set; }
    public string HeaderCourseTitle { get; set; } = string.Empty;
    public string HeaderGroupCode { get; set; } = string.Empty;
    public DateTime HeaderLessonDate { get; set; }
    public List<TeacherLessonCourseOptionVm> Courses { get; set; } = new();
    public List<TeacherLessonGroupOptionVm> Groups { get; set; } = new();
    public UpdateTeacherLessonViewModel Form { get; set; } = new();
}

public class UpdateTeacherLessonViewModel
{
    public int ScheduleId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Выберите курс.")]
    public int CourseId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Выберите группу.")]
    public int GroupId { get; set; }

    [Required(ErrorMessage = "Укажите дату урока.")]
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
    [RegularExpression(@"^https://.+", ErrorMessage = "Ссылка должна начинаться с https://")]
    public string? ConferenceUrl { get; set; }

    [Required(ErrorMessage = "Выберите статус урока.")]
    [RegularExpression("planned|live|finished", ErrorMessage = "Недопустимый статус урока.")]
    public string LiveStatus { get; set; } = AppConstants.LessonLiveStatus.Planned;
}
public class TeacherLessonCardVm
{
    public int ScheduleId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string GroupCode { get; set; } = string.Empty;
    public DateTime LessonDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? Topic { get; set; }
    public string? Room { get; set; }
    public string? ConferenceUrl { get; set; }
    public string LiveStatus { get; set; } = AppConstants.LessonLiveStatus.Planned;
    public int MaterialsCount { get; set; }
    public int SubmissionsCount { get; set; }
}

public class TeacherLessonAttendanceViewModel
{
    public int ScheduleId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string GroupCode { get; set; } = string.Empty;
    public DateTime LessonDate { get; set; }
    public List<TeacherAttendanceItemVm> Items { get; set; } = new();
}

public class TeacherAttendanceItemVm
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public bool IsPresent { get; set; }
}
public class TeacherLessonMaterialsViewModel
{
    public int ScheduleId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string GroupCode { get; set; } = string.Empty;
    public DateTime LessonDate { get; set; }
    public string? Topic { get; set; }
    public List<TeacherLessonMaterialVm> Materials { get; set; } = new();
    public CreateLessonMaterialViewModel Form { get; set; } = new();
}

public class TeacherLessonMaterialVm
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public string CloudUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateLessonMaterialViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Некорректный урок.")]
    public int ScheduleId { get; set; }

    [Required(ErrorMessage = "Укажите название материала.")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите тип материала.")]
    [RegularExpression("task|methodic|reference", ErrorMessage = "Недопустимый тип материала.")]
    public string MaterialType { get; set; } = AppConstants.LessonMaterialType.Task;

    [Required(ErrorMessage = "Укажите ссылку на облако.")]
    [StringLength(1000)]
    [RegularExpression(@"^https://.+", ErrorMessage = "Ссылка должна начинаться с https://")]
    public string CloudUrl { get; set; } = string.Empty;

    [Range(0, 1000, ErrorMessage = "Порядок должен быть от 0 до 1000.")]
    public int SortOrder { get; set; } = 0;
}

public class TeacherSubmissionsViewModel
{
    public string? StatusFilter { get; set; }
    public List<TeacherSubmissionItemVm> Items { get; set; } = new();
}

public class TeacherSubmissionItemVm
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string GroupCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public DateTime LessonDate { get; set; }
    public string CloudFileUrl { get; set; } = string.Empty;
    public string? StudentComment { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? Score { get; set; }
    public string? TeacherComment { get; set; }
    public int RevisionNumber { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class ReviewSubmissionViewModel
{
    [Required(ErrorMessage = "Выберите статус проверки.")]
    [RegularExpression("in_review|revision|accepted", ErrorMessage = "Недопустимый статус.")]
    public string Status { get; set; } = AppConstants.SubmissionStatus.InReview;

    [Range(0, 5, ErrorMessage = "Оценка должна быть в диапазоне 0-5.")]
    public decimal? Score { get; set; }

    [StringLength(2000)]
    public string? TeacherComment { get; set; }
}

public class StudentSubmissionsViewModel
{
    public bool HasLinkedProfile { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public List<StudentSubmissionLessonVm> AvailableLessons { get; set; } = new();
    public List<StudentSubmissionItemVm> Items { get; set; } = new();
    public CreateStudentSubmissionViewModel Form { get; set; } = new();
}

public class StudentSubmissionLessonVm
{
    public int ScheduleId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public DateTime LessonDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public string? Topic { get; set; }
    public string LiveStatus { get; set; } = AppConstants.LessonLiveStatus.Planned;
}

public class StudentSubmissionItemVm
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public DateTime LessonDate { get; set; }
    public string CloudFileUrl { get; set; } = string.Empty;
    public string? StudentComment { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? Score { get; set; }
    public string? TeacherComment { get; set; }
    public int RevisionNumber { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class CreateStudentSubmissionViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Выберите урок.")]
    public int ScheduleId { get; set; }

    [Required(ErrorMessage = "Укажите ссылку на файл.")]
    [StringLength(1000)]
    [RegularExpression(@"^https://.+", ErrorMessage = "Ссылка должна начинаться с https://")]
    public string CloudFileUrl { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? StudentComment { get; set; }
}


