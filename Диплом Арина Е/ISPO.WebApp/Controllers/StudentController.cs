using ISPO.WebApp.Data;
using ISPO.WebApp.Filters;
using ISPO.WebApp.Infrastructure;
using ISPO.WebApp.Models;
using ISPO.WebApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ISPO.WebApp.Controllers;

[SessionAuthorize("student", "admin")]
public class StudentController : Controller
{
    private readonly DiplomIspoDbContext _db;

    public StudentController(DiplomIspoDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? calendarView = "week", DateTime? calendarDate = null, int? calendarCourseId = null, int? calendarGroupId = null)
    {
        var sessionName = HttpContext.Session.GetString(AppSession.UserName) ?? "Ученик";
        var today = DateTime.Today;
        var normalizedCalendarView = NormalizeCalendarViewMode(calendarView);
        var calendarAnchor = (calendarDate ?? today).Date;
        var profile = await ResolveStudentProfileAsync();
        var student = profile.Student;

        if (student is null)
        {
            var (emptyRangeStart, emptyRangeEnd) = ResolveCalendarRange(normalizedCalendarView, calendarAnchor);
            return View(new StudentDashboardViewModel
            {
                UserFullName = sessionName,
                StudentName = "Профиль не найден",
                GroupCode = "-",
                GroupName = "Нет данных",
                Specialization = "Нет данных",
                IsDemoProfile = true,
                HasLinkedProfile = false,
                Calendar = new LessonCalendarViewModel
                {
                    ViewMode = normalizedCalendarView,
                    AnchorDate = calendarAnchor,
                    RangeStart = emptyRangeStart,
                    RangeEndInclusive = emptyRangeEnd,
                    RangeLabel = BuildCalendarRangeLabel(normalizedCalendarView, calendarAnchor, emptyRangeStart, emptyRangeEnd),
                    Days = BuildCalendarDays(
                        emptyRangeStart,
                        emptyRangeEnd,
                        normalizedCalendarView,
                        calendarAnchor,
                        new Dictionary<DateTime, List<CalendarLessonVm>>())
                }
            });
        }

        var studentId = student.Id;
        var schedulesForCalendarQuery = _db.Schedules
            .AsNoTracking()
            .Where(s => s.GroupId == student.GroupId);

        var averageGrade = await _db.LessonSubmissions
            .AsNoTracking()
            .Where(s => s.StudentId == studentId && s.Score != null && s.Status == AppConstants.SubmissionStatus.Accepted)
            .Select(s => s.Score)
            .AverageAsync();

        var attendanceStats = await _db.Attendances
            .AsNoTracking()
            .Where(a => a.StudentId == studentId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Present = g.Count(x => x.IsPresent)
            })
            .FirstOrDefaultAsync();

        var activeCoursesCount = await _db.Enrollments
            .AsNoTracking()
            .CountAsync(e => e.StudentId == studentId && e.Status == "active" && e.Course.IsActive);

        if (activeCoursesCount == 0)
        {
            activeCoursesCount = await _db.Schedules
                .AsNoTracking()
                .Where(s => s.GroupId == student.GroupId)
                .Select(s => s.CourseId)
                .Distinct()
                .CountAsync();
        }

        var recentGrades = await _db.LessonSubmissions
            .AsNoTracking()
            .Where(s => s.StudentId == studentId && s.Score != null && s.Status == AppConstants.SubmissionStatus.Accepted)
            .Include(s => s.Schedule)
            .ThenInclude(sc => sc.Course)
            .OrderByDescending(s => s.ReviewedAt ?? s.SubmittedAt)
            .Take(8)
            .Select(s => new StudentGradeVm
            {
                CourseTitle = s.Schedule.Course.Title,
                GradeValue = s.Score ?? 0,
                GradeDate = (s.ReviewedAt ?? s.SubmittedAt).Date,
                Comment = s.TeacherComment
            })
            .ToListAsync();

        var vm = new StudentDashboardViewModel
        {
            UserFullName = sessionName,
            StudentName = $"{student.LastName} {student.FirstName} {student.MiddleName}".Trim(),
            GroupCode = student.Group.GroupCode,
            GroupName = student.Group.Name,
            Specialization = student.Group.Specialization,
            IsDemoProfile = profile.IsDemoProfile,
            HasLinkedProfile = profile.HasLinkedProfile,
            ActiveCoursesCount = activeCoursesCount,
            AverageGrade = averageGrade is decimal avg ? decimal.Round(avg, 1) : null,
            AttendanceRate = attendanceStats is null || attendanceStats.Total == 0
                ? 0
                : decimal.Round(attendanceStats.Present * 100m / attendanceStats.Total, 1),
            Calendar = await BuildStudentCalendarAsync(
                schedulesForCalendarQuery,
                student,
                normalizedCalendarView,
                calendarAnchor,
                calendarCourseId,
                calendarGroupId),
            EnrolledCourses = await _db.Enrollments
                .AsNoTracking()
                .Where(e => e.StudentId == studentId)
                .Include(e => e.Course)
                .ThenInclude(c => c.Teacher)
                .OrderByDescending(e => e.EnrolledAt)
                .Select(e => new StudentCourseVm
                {
                    CourseCode = e.Course.CourseCode,
                    CourseTitle = e.Course.Title,
                    TeacherName = e.Course.Teacher.FullName,
                    DurationHours = e.Course.DurationHours,
                    Status = e.Status
                })
                .ToListAsync(),
            RecentGrades = recentGrades,
            UpcomingLessons = await _db.Schedules
                .AsNoTracking()
                .Where(s => s.GroupId == student.GroupId && s.LessonDate >= today)
                .Include(s => s.Course)
                .OrderBy(s => s.LessonDate)
                .ThenBy(s => s.StartTime)
                .Take(8)
                .Select(s => new StudentLessonVm
                {
                    ScheduleId = s.Id,
                    LessonDate = s.LessonDate,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    CourseTitle = s.Course.Title,
                    Room = s.Room,
                    Topic = s.LessonTopic,
                    LiveStatus = s.LiveStatus,
                    ConferenceUrl = s.ConferenceUrl
                })
                .ToListAsync(),
            LiveLessons = await _db.Schedules
                .AsNoTracking()
                .Where(s => s.GroupId == student.GroupId && s.LiveStatus == AppConstants.LessonLiveStatus.Live)
                .Include(s => s.Course)
                .OrderBy(s => s.LessonDate)
                .ThenBy(s => s.StartTime)
                .Select(s => new StudentLiveLessonVm
                {
                    ScheduleId = s.Id,
                    CourseTitle = s.Course.Title,
                    LessonDate = s.LessonDate,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    ConferenceUrl = s.ConferenceUrl
                })
                .ToListAsync(),
            LessonMaterials = await _db.LessonMaterials
                .AsNoTracking()
                .Where(m => m.Schedule.GroupId == student.GroupId)
                .Include(m => m.Schedule)
                .ThenInclude(s => s.Course)
                .OrderByDescending(m => m.Schedule.LessonDate)
                .ThenBy(m => m.SortOrder)
                .Take(8)
                .Select(m => new StudentMaterialVm
                {
                    Id = m.Id,
                    ScheduleId = m.ScheduleId,
                    LessonTitle = m.Schedule.LessonTopic ?? $"Занятие {m.Schedule.LessonDate:dd.MM.yyyy}",
                    CourseTitle = m.Schedule.Course.Title,
                    MaterialTitle = m.Title,
                    MaterialType = m.MaterialType,
                    CloudUrl = m.CloudUrl
                })
                .ToListAsync(),
            LatestSubmissions = await _db.LessonSubmissions
                .AsNoTracking()
                .Where(s => s.StudentId == studentId)
                .Include(s => s.Schedule)
                .ThenInclude(sc => sc.Course)
                .OrderByDescending(s => s.SubmittedAt)
                .Take(6)
                .Select(s => new StudentSubmissionShortVm
                {
                    Id = s.Id,
                    CourseTitle = s.Schedule.Course.Title,
                    LessonDate = s.Schedule.LessonDate,
                    Status = s.Status,
                    Score = s.Score,
                    RevisionNumber = s.RevisionNumber,
                    SubmittedAt = s.SubmittedAt
                })
                .ToListAsync()
        };

        return View(vm);
    }

    [HttpGet("/Student/Lessons")]
    public async Task<IActionResult> Lessons(string? calendarView = "week", DateTime? calendarDate = null, int? calendarCourseId = null, int? calendarGroupId = null)
    {
        var result = await Index(calendarView, calendarDate, calendarCourseId, calendarGroupId);
        if (result is ViewResult viewResult)
        {
            viewResult.ViewName = "Lessons";
            return viewResult;
        }

        return result;
    }

    [HttpGet("/Student/Submissions")]
    public async Task<IActionResult> Submissions()
    {
        var profile = await ResolveStudentProfileAsync(requireLinkedProfile: true);
        if (profile.Student is null || !profile.HasLinkedProfile)
        {
            TempData["Error"] = "Личный профиль студента не привязан к вашей учетной записи.";
            return View(new StudentSubmissionsViewModel { HasLinkedProfile = false, StudentName = "Профиль не найден" });
        }

        var vm = await BuildStudentSubmissionsViewModelAsync(profile.Student, new CreateStudentSubmissionViewModel());
        return View(vm);
    }

    [HttpPost("/Student/Submissions")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSubmission([Bind(Prefix = "Form")] CreateStudentSubmissionViewModel form)
    {
        var profile = await ResolveStudentProfileAsync(requireLinkedProfile: true);
        if (profile.Student is null || !profile.HasLinkedProfile)
        {
            TempData["Error"] = "Нельзя отправить работу: профиль студента не привязан.";
            return RedirectToAction(nameof(Submissions));
        }

        var student = profile.Student;
        var schedule = await _db.Schedules
            .AsNoTracking()
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == form.ScheduleId && s.GroupId == student.GroupId);
        if (schedule is null)
        {
            TempData["Error"] = "Урок не найден или недоступен для вашей группы.";
            return RedirectToAction(nameof(Submissions));
        }

        if (!ModelState.IsValid)
            return View("Submissions", await BuildStudentSubmissionsViewModelAsync(student, form));

        var existing = await _db.LessonSubmissions
            .FirstOrDefaultAsync(s => s.ScheduleId == form.ScheduleId && s.StudentId == student.Id);

        if (existing is null)
        {
            _db.LessonSubmissions.Add(new LessonSubmission
            {
                ScheduleId = form.ScheduleId,
                StudentId = student.Id,
                CloudFileUrl = form.CloudFileUrl.Trim(),
                StudentComment = string.IsNullOrWhiteSpace(form.StudentComment) ? null : form.StudentComment.Trim(),
                Status = AppConstants.SubmissionStatus.New,
                RevisionNumber = 1,
                SubmittedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.CloudFileUrl = form.CloudFileUrl.Trim();
            existing.StudentComment = string.IsNullOrWhiteSpace(form.StudentComment) ? null : form.StudentComment.Trim();
            existing.Status = AppConstants.SubmissionStatus.New;
            existing.Score = null;
            existing.TeacherComment = null;
            existing.ReviewedAt = null;
            existing.RevisionNumber += 1;
            existing.SubmittedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Работа отправлена преподавателю.";
        return RedirectToAction(nameof(Submissions));
    }

    private async Task<StudentSubmissionsViewModel> BuildStudentSubmissionsViewModelAsync(Student student, CreateStudentSubmissionViewModel form)
    {
        return new StudentSubmissionsViewModel
        {
            HasLinkedProfile = true,
            StudentName = $"{student.LastName} {student.FirstName} {student.MiddleName}".Trim(),
            AvailableLessons = await _db.Schedules
                .AsNoTracking()
                .Where(s => s.GroupId == student.GroupId)
                .Include(s => s.Course)
                .OrderByDescending(s => s.LessonDate)
                .ThenBy(s => s.StartTime)
                .Select(s => new StudentSubmissionLessonVm
                {
                    ScheduleId = s.Id,
                    CourseTitle = s.Course.Title,
                    LessonDate = s.LessonDate,
                    StartTime = s.StartTime,
                    Topic = s.LessonTopic,
                    LiveStatus = s.LiveStatus
                })
                .ToListAsync(),
            Items = await _db.LessonSubmissions
                .AsNoTracking()
                .Where(s => s.StudentId == student.Id)
                .Include(s => s.Schedule)
                .ThenInclude(sc => sc.Course)
                .OrderByDescending(s => s.SubmittedAt)
                .Select(s => new StudentSubmissionItemVm
                {
                    Id = s.Id,
                    ScheduleId = s.ScheduleId,
                    CourseTitle = s.Schedule.Course.Title,
                    LessonDate = s.Schedule.LessonDate,
                    CloudFileUrl = s.CloudFileUrl,
                    StudentComment = s.StudentComment,
                    Status = s.Status,
                    Score = s.Score,
                    TeacherComment = s.TeacherComment,
                    RevisionNumber = s.RevisionNumber,
                    SubmittedAt = s.SubmittedAt,
                    ReviewedAt = s.ReviewedAt
                })
                .ToListAsync(),
            Form = form
        };
    }

    private async Task<LessonCalendarViewModel> BuildStudentCalendarAsync(
        IQueryable<Schedule> schedulesQuery,
        Student student,
        string viewMode,
        DateTime anchorDate,
        int? selectedCourseId,
        int? selectedGroupId)
    {
        var normalizedView = NormalizeCalendarViewMode(viewMode);
        var anchor = anchorDate.Date;

        var courseOptions = await schedulesQuery
            .Select(s => new CalendarFilterOptionVm
            {
                Id = s.CourseId,
                Label = s.Course.Title
            })
            .Distinct()
            .OrderBy(c => c.Label)
            .ToListAsync();

        var groupOptions = new List<CalendarFilterOptionVm>
        {
            new()
            {
                Id = student.GroupId,
                Label = student.Group.GroupCode + " - " + student.Group.Name
            }
        };

        if (selectedCourseId.HasValue && courseOptions.All(c => c.Id != selectedCourseId.Value))
            selectedCourseId = null;
        if (selectedGroupId.HasValue && selectedGroupId.Value != student.GroupId)
            selectedGroupId = student.GroupId;
        else
            selectedGroupId ??= student.GroupId;

        var (rangeStart, rangeEndInclusive) = ResolveCalendarRange(normalizedView, anchor);

        var filteredQuery = schedulesQuery
            .Where(s => s.LessonDate >= rangeStart && s.LessonDate <= rangeEndInclusive);

        if (selectedCourseId.HasValue)
            filteredQuery = filteredQuery.Where(s => s.CourseId == selectedCourseId.Value);
        if (selectedGroupId.HasValue)
            filteredQuery = filteredQuery.Where(s => s.GroupId == selectedGroupId.Value);

        var lessonRows = await filteredQuery
            .OrderBy(s => s.LessonDate)
            .ThenBy(s => s.StartTime)
            .Select(s => new
            {
                LessonDate = s.LessonDate,
                Lesson = new CalendarLessonVm
                {
                    ScheduleId = s.Id,
                    CourseTitle = s.Course.Title,
                    GroupCode = s.Group.GroupCode,
                    Topic = s.LessonTopic,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    LiveStatus = s.LiveStatus
                }
            })
            .ToListAsync();

        var lessonsByDate = lessonRows
            .GroupBy(x => x.LessonDate.Date)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderBy(x => x.Lesson.StartTime)
                    .Select(x => x.Lesson)
                    .ToList());

        return new LessonCalendarViewModel
        {
            ViewMode = normalizedView,
            AnchorDate = anchor,
            RangeStart = rangeStart,
            RangeEndInclusive = rangeEndInclusive,
            RangeLabel = BuildCalendarRangeLabel(normalizedView, anchor, rangeStart, rangeEndInclusive),
            SelectedCourseId = selectedCourseId,
            SelectedGroupId = selectedGroupId,
            Courses = courseOptions,
            Groups = groupOptions,
            Days = BuildCalendarDays(rangeStart, rangeEndInclusive, normalizedView, anchor, lessonsByDate)
        };
    }

    private static string NormalizeCalendarViewMode(string? mode)
        => string.Equals(mode, "month", StringComparison.OrdinalIgnoreCase) ? "month" : "week";

    private static (DateTime RangeStart, DateTime RangeEndInclusive) ResolveCalendarRange(string viewMode, DateTime anchorDate)
    {
        var anchor = anchorDate.Date;
        if (viewMode == "month")
        {
            var monthStart = new DateTime(anchor.Year, anchor.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            return (StartOfWeek(monthStart), EndOfWeek(monthEnd));
        }

        var weekStart = StartOfWeek(anchor);
        return (weekStart, weekStart.AddDays(6));
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff).Date;
    }

    private static DateTime EndOfWeek(DateTime date)
        => StartOfWeek(date).AddDays(6);

    private static string BuildCalendarRangeLabel(string viewMode, DateTime anchorDate, DateTime rangeStart, DateTime rangeEndInclusive)
    {
        if (viewMode == "month")
        {
            var ru = CultureInfo.GetCultureInfo("ru-RU");
            return ru.TextInfo.ToTitleCase(anchorDate.ToString("MMMM yyyy", ru));
        }

        return $"{rangeStart:dd.MM.yyyy} - {rangeEndInclusive:dd.MM.yyyy}";
    }

    private static List<CalendarDayVm> BuildCalendarDays(
        DateTime rangeStart,
        DateTime rangeEndInclusive,
        string viewMode,
        DateTime anchorDate,
        IReadOnlyDictionary<DateTime, List<CalendarLessonVm>> lessonsByDate)
    {
        var today = DateTime.Today;
        var days = new List<CalendarDayVm>();
        var current = rangeStart.Date;

        while (current <= rangeEndInclusive.Date)
        {
            days.Add(new CalendarDayVm
            {
                Date = current,
                IsToday = current == today,
                IsCurrentPeriod = viewMode == "month"
                    ? current.Month == anchorDate.Month && current.Year == anchorDate.Year
                    : true,
                Lessons = lessonsByDate.TryGetValue(current, out var dayLessons) ? dayLessons : []
            });

            current = current.AddDays(1);
        }

        return days;
    }

    private async Task<(Student? Student, bool HasLinkedProfile, bool IsDemoProfile)> ResolveStudentProfileAsync(bool requireLinkedProfile = false)
    {
        var sessionEmail = NormalizeEmail(HttpContext.Session.GetString(AppSession.UserEmail));

        Student? linkedStudent = null;
        if (!string.IsNullOrWhiteSpace(sessionEmail))
        {
            linkedStudent = await _db.Students
                .AsNoTracking()
                .Include(s => s.Group)
                .FirstOrDefaultAsync(s => s.Email != null && s.Email.Trim().ToLower() == sessionEmail);
        }

        if (linkedStudent is not null)
            return (linkedStudent, true, false);

        if (!string.IsNullOrWhiteSpace(sessionEmail))
        {
            linkedStudent = await EnsureStudentProfileForCurrentUserAsync(sessionEmail);
            if (linkedStudent is not null)
                return (linkedStudent, true, false);
        }

        if (requireLinkedProfile)
            return (null, false, false);

        var demoStudent = await _db.Students
            .AsNoTracking()
            .Include(s => s.Group)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .FirstOrDefaultAsync();

        if (demoStudent is null)
            return (null, false, true);

        return (demoStudent, false, true);
    }

    private async Task<Student?> EnsureStudentProfileForCurrentUserAsync(string normalizedEmail)
    {
        var role = HttpContext.Session.GetString(AppSession.UserRole)?.Trim().ToLowerInvariant();
        if (role != "student")
            return null;

        var userId = HttpContext.Session.GetInt32(AppSession.UserId);
        if (!userId.HasValue)
            return null;

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == userId.Value &&
                u.IsActive &&
                u.Role == "student" &&
                u.Email != null &&
                u.Email.Trim().ToLower() == normalizedEmail);

        if (user is null)
            return null;

        var defaultGroupId = await _db.StudentGroups
            .AsNoTracking()
            .OrderBy(g => g.Id)
            .Select(g => g.Id)
            .FirstOrDefaultAsync();

        if (defaultGroupId <= 0)
            return null;

        var nameParts = SplitFullName(user.FullName);

        var student = new Student
        {
            LastName = nameParts.lastName,
            FirstName = nameParts.firstName,
            MiddleName = nameParts.middleName,
            BirthDate = DateTime.Today.AddYears(-18),
            Email = normalizedEmail,
            GroupId = defaultGroupId
        };

        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        return await _db.Students
            .AsNoTracking()
            .Include(s => s.Group)
            .FirstOrDefaultAsync(s => s.Id == student.Id);
    }

    private static (string lastName, string firstName, string? middleName) SplitFullName(string? fullName)
    {
        var parts = (fullName ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return ("Ученик", "Новый", null);

        var lastName = parts[0];
        var firstName = parts.Length > 1 ? parts[1] : "Новый";
        var middleName = parts.Length > 2 ? string.Join(' ', parts.Skip(2)) : null;

        return (lastName, firstName, middleName);
    }

    private static string NormalizeEmail(string? email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();
}

