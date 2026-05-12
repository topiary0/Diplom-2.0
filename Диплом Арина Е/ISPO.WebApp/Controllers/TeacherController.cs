using ISPO.WebApp.Data;
using ISPO.WebApp.Filters;
using ISPO.WebApp.Infrastructure;
using ISPO.WebApp.Models;
using ISPO.WebApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ISPO.WebApp.Controllers;

[SessionAuthorize("teacher", "admin")]
public class TeacherController : Controller
{
    private readonly DiplomIspoDbContext _db;

    public TeacherController(DiplomIspoDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? calendarView = "week", DateTime? calendarDate = null, int? calendarCourseId = null, int? calendarGroupId = null)
    {
        var today = DateTime.Today;
        var weekEnd = today.AddDays(7);
        var teacherScopeId = await GetTeacherScopeIdAsync();
        var normalizedCalendarView = NormalizeCalendarViewMode(calendarView);
        var calendarAnchor = (calendarDate ?? today).Date;

        var schedulesQuery = _db.Schedules
            .AsNoTracking()
            .Include(s => s.Course)
            .Include(s => s.Group)
            .AsQueryable();

        if (teacherScopeId.HasValue)
        {
            if (teacherScopeId.Value <= 0)
                schedulesQuery = schedulesQuery.Where(_ => false);
            else
                schedulesQuery = schedulesQuery.Where(s => s.Course.TeacherId == teacherScopeId.Value);
        }

        var scheduledGroupIdsQuery = schedulesQuery
            .Select(s => s.GroupId)
            .Distinct();

        var coursesQuery = _db.Courses
            .AsNoTracking()
            .AsQueryable();

        if (teacherScopeId.HasValue)
        {
            if (teacherScopeId.Value <= 0)
                coursesQuery = coursesQuery.Where(_ => false);
            else
                coursesQuery = coursesQuery.Where(c => c.TeacherId == teacherScopeId.Value);
        }

        var submissionsQuery = _db.LessonSubmissions
            .AsNoTracking()
            .AsQueryable();

        if (teacherScopeId.HasValue)
        {
            if (teacherScopeId.Value <= 0)
                submissionsQuery = submissionsQuery.Where(_ => false);
            else
                submissionsQuery = submissionsQuery.Where(s => s.Schedule.Course.TeacherId == teacherScopeId.Value);
        }

        var vm = new TeacherPanelViewModel
        {
            UserFullName = HttpContext.Session.GetString(AppSession.UserName) ?? "Преподаватель",
            StudentsCount = await _db.Students
                .AsNoTracking()
                .CountAsync(st => scheduledGroupIdsQuery.Contains(st.GroupId)),
            CoursesCount = await coursesQuery.CountAsync(),
            GroupsCount = await scheduledGroupIdsQuery.CountAsync(),
            LessonsCount = await schedulesQuery.CountAsync(),
            LessonsThisWeek = await schedulesQuery.CountAsync(s => s.LessonDate >= today && s.LessonDate < weekEnd),
            LiveLessonsCount = await schedulesQuery.CountAsync(s => s.LiveStatus == AppConstants.LessonLiveStatus.Live),
            PendingSubmissionsCount = await submissionsQuery.CountAsync(s =>
                s.Status == AppConstants.SubmissionStatus.New || s.Status == AppConstants.SubmissionStatus.InReview),
            UpcomingLessons = await schedulesQuery
                .Where(s => s.LessonDate >= today)
                .OrderBy(s => s.LessonDate)
                .ThenBy(s => s.StartTime)
                .Take(8)
                .Select(s => new TeacherLessonVm
                {
                    ScheduleId = s.Id,
                    CourseTitle = s.Course.Title,
                    GroupCode = s.Group.GroupCode,
                    LessonDate = s.LessonDate,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Room = s.Room,
                    Topic = s.LessonTopic,
                    LiveStatus = s.LiveStatus
                })
                .ToListAsync(),
            Calendar = await BuildTeacherCalendarAsync(
                schedulesQuery,
                normalizedCalendarView,
                calendarAnchor,
                calendarCourseId,
                calendarGroupId)
        };

        if (teacherScopeId is <= 0)
            TempData["Error"] = "Профиль преподавателя не найден по адресу электронной почты. Некоторые функции ограничены.";

        return View(vm);
    }

    public IActionResult Students()
    {
        TempData["Error"] = "Добавление студентов выполняет администратор через раздел \"Пользователи\".";
        return RedirectToAction("Users", "Admin");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateStudent([Bind(Prefix = "Form")] CreateStudentViewModel form)
    {
        TempData["Error"] = "Преподаватель не может создавать студентов. Обратитесь к администратору.";
        return RedirectToAction("Users", "Admin");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteStudent(int id)
    {
        TempData["Error"] = "Удаление студентов доступно только администратору.";
        return RedirectToAction("Users", "Admin");
    }

    public async Task<IActionResult> Courses()
    {
        var vm = await BuildCoursesViewModelAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateCourse([Bind(Prefix = "Form")] CreateCourseViewModel form)
    {
        TempData["Error"] = "Создание курсов выполняется администратором. Преподавателю доступен только просмотр.";
        return RedirectToAction(nameof(Courses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteCourse(int id)
    {
        TempData["Error"] = "Удаление курсов выполняется администратором.";
        return RedirectToAction(nameof(Courses));
    }

    [HttpGet("/Teacher/Lessons")]
    public async Task<IActionResult> Lessons()
    {
        var teacherScopeId = await GetTeacherScopeIdAsync();
        if (teacherScopeId is <= 0)
        {
            TempData["Error"] = "Профиль преподавателя не найден. Раздел уроков недоступен.";
            return View(new TeacherLessonsViewModel());
        }

        var vm = await BuildLessonsViewModelAsync(teacherScopeId.Value);
        return View(vm);
    }

    [HttpPost("/Teacher/Lessons")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLesson([Bind(Prefix = "Form")] CreateTeacherLessonViewModel form)
    {
        var teacherScopeId = await GetTeacherScopeIdAsync();
        if (teacherScopeId is <= 0)
        {
            TempData["Error"] = "Профиль преподавателя не найден. Нельзя создать урок.";
            return RedirectToAction(nameof(Lessons));
        }

        ValidateLessonForm(form.StartTime, form.EndTime, form.ConferenceUrl);

        var canUseCourse = await _db.Courses
            .AsNoTracking()
            .AnyAsync(c => c.Id == form.CourseId && c.TeacherId == teacherScopeId.Value && c.IsActive);
        if (!canUseCourse)
            ModelState.AddModelError("Form.CourseId", "Выберите курс из вашего списка.");

        var groupExists = await _db.StudentGroups
            .AsNoTracking()
            .AnyAsync(g => g.Id == form.GroupId);
        if (!groupExists)
            ModelState.AddModelError("Form.GroupId", "Выбранная группа не найдена.");

        if (!ModelState.IsValid)
            return View("Lessons", await BuildLessonsViewModelAsync(teacherScopeId.Value, form));

        _db.Schedules.Add(new Schedule
        {
            CourseId = form.CourseId,
            GroupId = form.GroupId,
            LessonDate = form.LessonDate.Date,
            StartTime = form.StartTime,
            EndTime = form.EndTime,
            Room = string.IsNullOrWhiteSpace(form.Room) ? null : form.Room.Trim(),
            LessonTopic = string.IsNullOrWhiteSpace(form.LessonTopic) ? null : form.LessonTopic.Trim(),
            ConferenceUrl = string.IsNullOrWhiteSpace(form.ConferenceUrl) ? null : form.ConferenceUrl.Trim(),
            LiveStatus = AppConstants.LessonLiveStatus.Planned
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = "Урок добавлен в расписание.";
        return RedirectToAction(nameof(Lessons));
    }

    [HttpGet("/Teacher/Lessons/{scheduleId:int}/edit")]
    public async Task<IActionResult> EditLesson(int scheduleId)
    {
        var schedule = await _db.Schedules
            .AsNoTracking()
            .Include(s => s.Course)
            .Include(s => s.Group)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        if (schedule is null || !await CanManageScheduleAsync(schedule))
        {
            TempData["Error"] = "Урок не найден или недоступен для редактирования.";
            return RedirectToAction(nameof(Lessons));
        }

        var vm = await BuildEditLessonViewModelAsync(scheduleId, new UpdateTeacherLessonViewModel
        {
            ScheduleId = schedule.Id,
            CourseId = schedule.CourseId,
            GroupId = schedule.GroupId,
            LessonDate = schedule.LessonDate,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            Room = schedule.Room,
            LessonTopic = schedule.LessonTopic,
            ConferenceUrl = schedule.ConferenceUrl,
            LiveStatus = schedule.LiveStatus
        });

        return View(vm);
    }

    [HttpPost("/Teacher/Lessons/{scheduleId:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditLesson(int scheduleId, [Bind(Prefix = "Form")] UpdateTeacherLessonViewModel form)
    {
        if (form.ScheduleId != scheduleId)
            form.ScheduleId = scheduleId;

        var schedule = await _db.Schedules
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        if (schedule is null || !await CanManageScheduleAsync(schedule))
        {
            TempData["Error"] = "Урок не найден или недоступен для редактирования.";
            return RedirectToAction(nameof(Lessons));
        }

        ValidateLessonForm(form.StartTime, form.EndTime, form.ConferenceUrl, "Form");

        var normalizedStatus = (form.LiveStatus ?? string.Empty).Trim().ToLowerInvariant();
        if (!AppConstants.LessonLiveStatus.All.Contains(normalizedStatus))
            ModelState.AddModelError("Form.LiveStatus", "Выберите корректный статус урока.");

        var teacherScopeId = await GetTeacherScopeIdAsync();
        if (teacherScopeId is <= 0)
        {
            TempData["Error"] = "Профиль преподавателя не найден. Нельзя изменить урок.";
            return RedirectToAction(nameof(Lessons));
        }

        var canUseCourse = await _db.Courses
            .AsNoTracking()
            .AnyAsync(c => c.Id == form.CourseId && c.TeacherId == teacherScopeId.Value && c.IsActive);
        if (!canUseCourse)
            ModelState.AddModelError("Form.CourseId", "Выберите курс из вашего списка.");

        var groupExists = await _db.StudentGroups
            .AsNoTracking()
            .AnyAsync(g => g.Id == form.GroupId);
        if (!groupExists)
            ModelState.AddModelError("Form.GroupId", "Выбранная группа не найдена.");

        if (!ModelState.IsValid)
            return View(await BuildEditLessonViewModelAsync(scheduleId, form));

        schedule.CourseId = form.CourseId;
        schedule.GroupId = form.GroupId;
        schedule.LessonDate = form.LessonDate.Date;
        schedule.StartTime = form.StartTime;
        schedule.EndTime = form.EndTime;
        schedule.Room = string.IsNullOrWhiteSpace(form.Room) ? null : form.Room.Trim();
        schedule.LessonTopic = string.IsNullOrWhiteSpace(form.LessonTopic) ? null : form.LessonTopic.Trim();
        schedule.ConferenceUrl = string.IsNullOrWhiteSpace(form.ConferenceUrl) ? null : form.ConferenceUrl.Trim();
        schedule.LiveStatus = normalizedStatus;

        if (normalizedStatus == AppConstants.LessonLiveStatus.Live)
        {
            schedule.LiveStartedAt ??= DateTime.UtcNow;
            schedule.LiveEndedAt = null;
        }
        else if (normalizedStatus == AppConstants.LessonLiveStatus.Finished)
        {
            schedule.LiveStartedAt ??= DateTime.UtcNow;
            schedule.LiveEndedAt ??= DateTime.UtcNow;
        }
        else
        {
            schedule.LiveStartedAt = null;
            schedule.LiveEndedAt = null;
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = "Изменения урока сохранены.";
        return RedirectToAction(nameof(Lessons));
    }

    [HttpPost("/Teacher/Lessons/{scheduleId:int}/start")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartLesson(int scheduleId)
    {
        var schedule = await _db.Schedules
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);
        if (schedule is null || !await CanManageScheduleAsync(schedule))
            return RedirectToAction(nameof(Lessons));

        if (string.IsNullOrWhiteSpace(schedule.ConferenceUrl))
        {
            TempData["Error"] = "Перед запуском урока укажите ссылку на конференцию.";
            return RedirectToAction(nameof(Lessons));
        }

        schedule.LiveStatus = AppConstants.LessonLiveStatus.Live;
        schedule.LiveStartedAt = DateTime.UtcNow;
        schedule.LiveEndedAt = null;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Урок переведен в статус \"Идет урок\".";
        return RedirectToAction(nameof(Lessons));
    }

    [HttpPost("/Teacher/Lessons/{scheduleId:int}/finish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FinishLesson(int scheduleId)
    {
        var schedule = await _db.Schedules
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);
        if (schedule is null || !await CanManageScheduleAsync(schedule))
            return RedirectToAction(nameof(Lessons));

        schedule.LiveStatus = AppConstants.LessonLiveStatus.Finished;
        schedule.LiveEndedAt = DateTime.UtcNow;
        if (schedule.LiveStartedAt is null)
            schedule.LiveStartedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Урок завершен.";
        return RedirectToAction(nameof(Lessons));
    }

    [HttpGet("/Teacher/Lessons/{scheduleId:int}/materials")]
    public async Task<IActionResult> LessonMaterials(int scheduleId)
    {
        var teacherScopeId = await GetTeacherScopeIdAsync();
        if (teacherScopeId is <= 0)
            return RedirectToAction(nameof(Lessons));

        var schedule = await _db.Schedules
            .AsNoTracking()
            .Include(s => s.Course)
            .Include(s => s.Group)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);
        if (schedule is null || !await CanManageScheduleAsync(schedule))
            return RedirectToAction(nameof(Lessons));

        var vm = new TeacherLessonMaterialsViewModel
        {
            ScheduleId = schedule.Id,
            CourseTitle = schedule.Course.Title,
            GroupCode = schedule.Group.GroupCode,
            LessonDate = schedule.LessonDate,
            Topic = schedule.LessonTopic,
            Materials = await _db.LessonMaterials
                .AsNoTracking()
                .Where(m => m.ScheduleId == scheduleId)
                .OrderBy(m => m.SortOrder)
                .ThenBy(m => m.CreatedAt)
                .Select(m => new TeacherLessonMaterialVm
                {
                    Id = m.Id,
                    Title = m.Title,
                    MaterialType = m.MaterialType,
                    CloudUrl = m.CloudUrl,
                    SortOrder = m.SortOrder,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync(),
            Form = new CreateLessonMaterialViewModel { ScheduleId = scheduleId }
        };

        return View(vm);
    }

    [HttpPost("/Teacher/Lessons/{scheduleId:int}/materials")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLessonMaterial(int scheduleId, [Bind(Prefix = "Form")] CreateLessonMaterialViewModel form)
    {
        if (form.ScheduleId != scheduleId)
            form.ScheduleId = scheduleId;

        var schedule = await _db.Schedules
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == form.ScheduleId);
        if (schedule is null || !await CanManageScheduleAsync(schedule))
            return RedirectToAction(nameof(Lessons));

        if (!ModelState.IsValid)
            return View("LessonMaterials", await BuildLessonMaterialViewModelAsync(form.ScheduleId, form));

        _db.LessonMaterials.Add(new LessonMaterial
        {
            ScheduleId = form.ScheduleId,
            Title = form.Title.Trim(),
            MaterialType = form.MaterialType.Trim().ToLowerInvariant(),
            CloudUrl = form.CloudUrl.Trim(),
            SortOrder = form.SortOrder
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Материал урока добавлен.";
        return RedirectToAction(nameof(LessonMaterials), new { scheduleId = form.ScheduleId });
    }

    [HttpPost("/Teacher/Lessons/{scheduleId:int}/materials/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLessonMaterial(int id, int scheduleId)
    {
        var material = await _db.LessonMaterials
            .Include(m => m.Schedule)
            .ThenInclude(s => s.Course)
            .FirstOrDefaultAsync(m => m.Id == id && m.ScheduleId == scheduleId);
        if (material is null || !await CanManageScheduleAsync(material.Schedule))
            return RedirectToAction(nameof(Lessons));

        _db.LessonMaterials.Remove(material);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Материал удален.";
        return RedirectToAction(nameof(LessonMaterials), new { scheduleId });
    }

    [HttpGet("/Teacher/Lessons/{scheduleId:int}/attendance")]
    public async Task<IActionResult> LessonAttendance(int scheduleId)
    {
        var schedule = await _db.Schedules
            .AsNoTracking()
            .Include(s => s.Course)
            .Include(s => s.Group)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        if (schedule is null || !await CanManageScheduleAsync(schedule))
        {
            TempData["Error"] = "Урок не найден или недоступен.";
            return RedirectToAction(nameof(Lessons));
        }

        var vm = await BuildLessonAttendanceViewModelAsync(scheduleId);
        return View(vm);
    }

    [HttpPost("/Teacher/Lessons/{scheduleId:int}/attendance")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLessonAttendance(int scheduleId, int[]? presentStudentIds)
    {
        var schedule = await _db.Schedules
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        if (schedule is null || !await CanManageScheduleAsync(schedule))
        {
            TempData["Error"] = "Урок не найден или недоступен.";
            return RedirectToAction(nameof(Lessons));
        }

        var groupStudentIds = await _db.Students
            .AsNoTracking()
            .Where(s => s.GroupId == schedule.GroupId)
            .Select(s => s.Id)
            .ToListAsync();

        var selected = new HashSet<int>(presentStudentIds ?? Array.Empty<int>());

        var attendanceRows = await _db.Attendances
            .Where(a => a.ScheduleId == scheduleId)
            .ToListAsync();

        foreach (var studentId in groupStudentIds)
        {
            var isPresent = selected.Contains(studentId);
            var row = attendanceRows.FirstOrDefault(a => a.StudentId == studentId);

            if (row is null)
            {
                _db.Attendances.Add(new Attendance
                {
                    ScheduleId = scheduleId,
                    StudentId = studentId,
                    IsPresent = isPresent,
                    MarkedAt = DateTime.UtcNow
                });
            }
            else
            {
                row.IsPresent = isPresent;
                row.MarkedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Посещаемость сохранена.";
        return RedirectToAction(nameof(LessonAttendance), new { scheduleId });
    }
[HttpGet("/Teacher/Submissions")]
    public async Task<IActionResult> Submissions(string? status = null)
    {
        var teacherScopeId = await GetTeacherScopeIdAsync();
        if (teacherScopeId is <= 0)
        {
            TempData["Error"] = "Профиль преподавателя не найден. Проверка работ недоступна.";
            return View(new TeacherSubmissionsViewModel());
        }

        var normalizedStatus = status?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalizedStatus) && !AppConstants.SubmissionStatus.All.Contains(normalizedStatus))
            normalizedStatus = null;

        var query = _db.LessonSubmissions
            .AsNoTracking()
            .Include(s => s.Student)
            .ThenInclude(st => st.Group)
            .Include(s => s.Schedule)
            .ThenInclude(sc => sc.Course)
            .AsQueryable();

        if (teacherScopeId.HasValue)
            query = query.Where(s => s.Schedule.Course.TeacherId == teacherScopeId.Value);
        if (!string.IsNullOrWhiteSpace(normalizedStatus))
            query = query.Where(s => s.Status == normalizedStatus);

        var vm = new TeacherSubmissionsViewModel
        {
            StatusFilter = normalizedStatus,
            Items = await query
                .OrderByDescending(s => s.SubmittedAt)
                .Select(s => new TeacherSubmissionItemVm
                {
                    Id = s.Id,
                    ScheduleId = s.ScheduleId,
                    StudentName = (s.Student.LastName + " " + s.Student.FirstName + " " + (s.Student.MiddleName ?? string.Empty)).Trim(),
                    GroupCode = s.Student.Group.GroupCode,
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
                .ToListAsync()
        };

        return View(vm);
    }

    [HttpPost("/Teacher/Submissions/{id:int}/review")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewSubmission(int id, [Bind(Prefix = "Form")] ReviewSubmissionViewModel form, string? statusFilter)
    {
        var submission = await _db.LessonSubmissions
            .Include(s => s.Schedule)
            .ThenInclude(sc => sc.Course)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (submission is null || !await CanManageScheduleAsync(submission.Schedule))
            return RedirectToAction(nameof(Submissions));

        var status = form.Status.Trim().ToLowerInvariant();
        if (!AppConstants.SubmissionStatus.All.Contains(status) || status == AppConstants.SubmissionStatus.New)
            ModelState.AddModelError("Form.Status", "Недопустимый статус проверки.");

        if (status == AppConstants.SubmissionStatus.Accepted && form.Score is null)
            ModelState.AddModelError("Form.Score", "Для статуса \"Принята\" укажите оценку.");

        if (form.Score is < 0 or > 5)
            ModelState.AddModelError("Form.Score", "Оценка должна быть в диапазоне 0-5.");

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Не удалось сохранить проверку. Проверьте поля формы.";
            return RedirectToAction(nameof(Submissions), new { status = statusFilter });
        }

        submission.Status = status;
        submission.Score = form.Score;
        submission.TeacherComment = string.IsNullOrWhiteSpace(form.TeacherComment) ? null : form.TeacherComment.Trim();
        submission.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Проверка работы сохранена.";
        return RedirectToAction(nameof(Submissions), new { status = statusFilter });
    }

    private void ValidateLessonForm(TimeSpan startTime, TimeSpan endTime, string? conferenceUrl, string prefix = "Form")
    {
        if (endTime <= startTime)
            ModelState.AddModelError($"{prefix}.EndTime", "Время окончания должно быть позже времени начала.");

        if (!string.IsNullOrWhiteSpace(conferenceUrl) &&
            !conferenceUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError($"{prefix}.ConferenceUrl", "Ссылка на конференцию должна начинаться с https://");
        }
    }

    private async Task<bool> CanManageScheduleAsync(Schedule schedule)
    {
        var teacherScopeId = await GetTeacherScopeIdAsync();
        if (!teacherScopeId.HasValue)
            return true;
        if (teacherScopeId.Value <= 0)
            return false;
        return schedule.Course.TeacherId == teacherScopeId.Value;
    }

    private async Task<int?> GetTeacherScopeIdAsync()
    {
        var role = HttpContext.Session.GetString(AppSession.UserRole)?.Trim().ToLowerInvariant();
        if (role == "admin")
            return null;

        var email = NormalizeEmail(HttpContext.Session.GetString(AppSession.UserEmail));
        if (string.IsNullOrWhiteSpace(email))
            return -1;

        var teacherId = await _db.Teachers
            .AsNoTracking()
            .Where(t => t.Email != null && t.Email.Trim().ToLower() == email)
            .Select(t => t.Id)
            .FirstOrDefaultAsync();

        if (teacherId > 0)
            return teacherId;

        var autoCreatedTeacher = await EnsureTeacherProfileForCurrentUserAsync(email);
        return autoCreatedTeacher?.Id ?? -1;
    }

    private async Task<Teacher?> EnsureTeacherProfileForCurrentUserAsync(string normalizedEmail)
    {
        var role = HttpContext.Session.GetString(AppSession.UserRole)?.Trim().ToLowerInvariant();
        if (role != "teacher")
            return null;

        var userId = HttpContext.Session.GetInt32(AppSession.UserId);
        if (!userId.HasValue)
            return null;

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == userId.Value &&
                u.IsActive &&
                u.Role.ToLower() == "teacher" &&
                u.Email != null &&
                u.Email.Trim().ToLower() == normalizedEmail);

        if (user is null)
            return null;

        var teacher = new Teacher
        {
            FullName = string.IsNullOrWhiteSpace(user.FullName) ? "Преподаватель" : user.FullName.Trim(),
            Email = normalizedEmail,
            Department = "Общее отделение",
            PositionName = "Преподаватель"
        };

        _db.Teachers.Add(teacher);
        await _db.SaveChangesAsync();

        return teacher;
    }

    private static string NormalizeEmail(string? email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();

    private async Task<LessonCalendarViewModel> BuildTeacherCalendarAsync(
        IQueryable<Schedule> schedulesQuery,
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

        var groupOptions = await schedulesQuery
            .Select(s => new CalendarFilterOptionVm
            {
                Id = s.GroupId,
                Label = s.Group.GroupCode + " - " + s.Group.Name
            })
            .Distinct()
            .OrderBy(g => g.Label)
            .ToListAsync();

        if (selectedCourseId.HasValue && courseOptions.All(c => c.Id != selectedCourseId.Value))
            selectedCourseId = null;
        if (selectedGroupId.HasValue && groupOptions.All(g => g.Id != selectedGroupId.Value))
            selectedGroupId = null;

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

        var today = DateTime.Today;
        var days = new List<CalendarDayVm>();
        var current = rangeStart;
        while (current <= rangeEndInclusive)
        {
            days.Add(new CalendarDayVm
            {
                Date = current,
                IsToday = current == today,
                IsCurrentPeriod = normalizedView == "month"
                    ? current.Month == anchor.Month && current.Year == anchor.Year
                    : true,
                Lessons = lessonsByDate.TryGetValue(current, out var dayLessons)
                    ? dayLessons
                    : []
            });
            current = current.AddDays(1);
        }

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
            Days = days
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

    private async Task<TeacherLessonsViewModel> BuildLessonsViewModelAsync(int teacherId, CreateTeacherLessonViewModel? form = null)
    {
        return new TeacherLessonsViewModel
        {
            Lessons = await _db.Schedules
                .AsNoTracking()
                .Include(s => s.Course)
                .Include(s => s.Group)
                .Include(s => s.LessonMaterials)
                .Include(s => s.LessonSubmissions)
                .Where(s => s.Course.TeacherId == teacherId)
                .OrderByDescending(s => s.LessonDate)
                .ThenBy(s => s.StartTime)
                .Select(s => new TeacherLessonCardVm
                {
                    ScheduleId = s.Id,
                    CourseTitle = s.Course.Title,
                    GroupCode = s.Group.GroupCode,
                    LessonDate = s.LessonDate,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Topic = s.LessonTopic,
                    Room = s.Room,
                    ConferenceUrl = s.ConferenceUrl,
                    LiveStatus = s.LiveStatus,
                    MaterialsCount = s.LessonMaterials.Count,
                    SubmissionsCount = s.LessonSubmissions.Count
                })
                .ToListAsync(),
            Courses = await _db.Courses
                .AsNoTracking()
                .Where(c => c.TeacherId == teacherId && c.IsActive)
                .OrderBy(c => c.Title)
                .Select(c => new TeacherLessonCourseOptionVm
                {
                    Id = c.Id,
                    Label = c.CourseCode + " - " + c.Title
                })
                .ToListAsync(),
            Groups = await _db.StudentGroups
                .AsNoTracking()
                .OrderBy(g => g.GroupCode)
                .Select(g => new TeacherLessonGroupOptionVm
                {
                    Id = g.Id,
                    Label = g.GroupCode + " - " + g.Name
                })
                .ToListAsync(),
            Form = form ?? new CreateTeacherLessonViewModel()
        };
    }

    private async Task<TeacherEditLessonViewModel> BuildEditLessonViewModelAsync(int scheduleId, UpdateTeacherLessonViewModel form)
    {
        var teacherScopeId = await GetTeacherScopeIdAsync();

        var scheduleInfo = await _db.Schedules
            .AsNoTracking()
            .Include(s => s.Course)
            .Include(s => s.Group)
            .Where(s => s.Id == scheduleId)
            .Select(s => new
            {
                s.Id,
                CourseTitle = s.Course.Title,
                GroupCode = s.Group.GroupCode,
                s.LessonDate,
                TeacherId = s.Course.TeacherId
            })
            .FirstAsync();

        var teacherId = teacherScopeId is > 0 ? teacherScopeId.Value : scheduleInfo.TeacherId;

        return new TeacherEditLessonViewModel
        {
            ScheduleId = scheduleId,
            HeaderCourseTitle = scheduleInfo.CourseTitle,
            HeaderGroupCode = scheduleInfo.GroupCode,
            HeaderLessonDate = scheduleInfo.LessonDate,
            Courses = await _db.Courses
                .AsNoTracking()
                .Where(c => c.TeacherId == teacherId && c.IsActive)
                .OrderBy(c => c.Title)
                .Select(c => new TeacherLessonCourseOptionVm
                {
                    Id = c.Id,
                    Label = c.CourseCode + " - " + c.Title
                })
                .ToListAsync(),
            Groups = await _db.StudentGroups
                .AsNoTracking()
                .OrderBy(g => g.GroupCode)
                .Select(g => new TeacherLessonGroupOptionVm
                {
                    Id = g.Id,
                    Label = g.GroupCode + " - " + g.Name
                })
                .ToListAsync(),
            Form = form
        };
    }

    private async Task<TeacherLessonAttendanceViewModel> BuildLessonAttendanceViewModelAsync(int scheduleId)
    {
        var schedule = await _db.Schedules
            .AsNoTracking()
            .Include(s => s.Course)
            .Include(s => s.Group)
            .FirstAsync(s => s.Id == scheduleId);

        var attendanceMap = await _db.Attendances
            .AsNoTracking()
            .Where(a => a.ScheduleId == scheduleId)
            .ToDictionaryAsync(a => a.StudentId, a => a.IsPresent);

        var students = await _db.Students
            .AsNoTracking()
            .Where(s => s.GroupId == schedule.GroupId)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Select(s => new
            {
                s.Id,
                s.LastName,
                s.FirstName,
                s.MiddleName
            })
            .ToListAsync();

        var items = students
            .Select(s => new TeacherAttendanceItemVm
            {
                StudentId = s.Id,
                StudentName = (s.LastName + " " + s.FirstName + " " + (s.MiddleName ?? string.Empty)).Trim(),
                IsPresent = attendanceMap.TryGetValue(s.Id, out var present) && present
            })
            .ToList();

        return new TeacherLessonAttendanceViewModel
        {
            ScheduleId = scheduleId,
            CourseTitle = schedule.Course.Title,
            GroupCode = schedule.Group.GroupCode,
            LessonDate = schedule.LessonDate,
            Items = items
        };
    }

    private async Task<TeacherLessonMaterialsViewModel> BuildLessonMaterialViewModelAsync(int scheduleId, CreateLessonMaterialViewModel? form = null)
    {
        var schedule = await _db.Schedules
            .AsNoTracking()
            .Include(s => s.Course)
            .Include(s => s.Group)
            .FirstAsync(s => s.Id == scheduleId);

        return new TeacherLessonMaterialsViewModel
        {
            ScheduleId = scheduleId,
            CourseTitle = schedule.Course.Title,
            GroupCode = schedule.Group.GroupCode,
            LessonDate = schedule.LessonDate,
            Topic = schedule.LessonTopic,
            Materials = await _db.LessonMaterials
                .AsNoTracking()
                .Where(m => m.ScheduleId == scheduleId)
                .OrderBy(m => m.SortOrder)
                .ThenBy(m => m.CreatedAt)
                .Select(m => new TeacherLessonMaterialVm
                {
                    Id = m.Id,
                    Title = m.Title,
                    MaterialType = m.MaterialType,
                    CloudUrl = m.CloudUrl,
                    SortOrder = m.SortOrder,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync(),
            Form = form ?? new CreateLessonMaterialViewModel { ScheduleId = scheduleId }
        };
    }

    private async Task<TeacherCoursesViewModel> BuildCoursesViewModelAsync(CreateCourseViewModel? form = null)
    {
        var teacherScopeId = await GetTeacherScopeIdAsync();
        var hasTeacherScope = teacherScopeId is > 0;

        var teachersQuery = _db.Teachers.AsNoTracking().AsQueryable();
        var coursesQuery = _db.Courses
            .AsNoTracking()
            .Include(c => c.Teacher)
            .Include(c => c.Category)
            .AsQueryable();

        if (hasTeacherScope)
        {
            teachersQuery = teachersQuery.Where(t => t.Id == teacherScopeId!.Value);
            coursesQuery = coursesQuery.Where(c => c.TeacherId == teacherScopeId!.Value);
        }
        else
        {
            teachersQuery = teachersQuery.Where(_ => false);
            coursesQuery = coursesQuery.Where(_ => false);
        }

        return new TeacherCoursesViewModel
        {
            UserFullName = HttpContext.Session.GetString(AppSession.UserName) ?? "Преподаватель",
            Teachers = await teachersQuery.OrderBy(t => t.FullName).ToListAsync(),
            Categories = await _db.CourseCategories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync(),
            Courses = await coursesQuery
                .OrderBy(c => c.Title)
                .Select(c => new CourseListItemVm
                {
                    Id = c.Id,
                    CourseCode = c.CourseCode,
                    Title = c.Title,
                    DurationHours = c.DurationHours,
                    IsActive = c.IsActive,
                    CategoryName = c.Category.Name,
                    TeacherName = c.Teacher.FullName
                })
                .ToListAsync(),
            Form = form ?? new CreateCourseViewModel
            {
                TeacherId = hasTeacherScope ? teacherScopeId!.Value : 0
            }
        };
    }
}






