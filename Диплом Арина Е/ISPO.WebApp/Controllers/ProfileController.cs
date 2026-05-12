using ISPO.WebApp.Data;
using ISPO.WebApp.Filters;
using ISPO.WebApp.Infrastructure;
using ISPO.WebApp.Models;
using ISPO.WebApp.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISPO.WebApp.Controllers;

[SessionAuthorize("admin", "teacher", "student")]
public class ProfileController : Controller
{
    private readonly DiplomIspoDbContext _db;
    private const string SiteThemeCookie = "ISPO.SiteTheme";
    private const string InterfaceModeCookie = "ISPO.InterfaceMode";
    private const string SiteThemePerUserPrefix = "ISPO.SiteTheme.User.";
    private const string InterfaceModePerUserPrefix = "ISPO.InterfaceMode.User.";

    public ProfileController(DiplomIspoDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var role = NormalizeRole(HttpContext.Session.GetString(AppSession.UserRole));
        return role switch
        {
            "admin" => RedirectToAction(nameof(Admin)),
            "teacher" => RedirectToAction(nameof(Teacher)),
            "student" => RedirectToAction(nameof(Student)),
            _ => RedirectToAction("Login", "Auth")
        };
    }

    [SessionAuthorize("admin")]
    [HttpGet]
    public async Task<IActionResult> Admin()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return RedirectToAction("Login", "Auth");

        var vm = await BuildAdminSettingsViewModelAsync(new UpdateAdminSettingsForm
        {
            FullName = user.FullName,
            Email = user.Email
        });

        return View(vm);
    }

    [SessionAuthorize("admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Admin([Bind(Prefix = "Form")] UpdateAdminSettingsForm form)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return RedirectToAction("Login", "Auth");

        var normalizedEmail = NormalizeEmail(form.Email);

        if (await _db.Users.AnyAsync(x => x.Id != user.Id && x.Email != null && x.Email.Trim().ToLower() == normalizedEmail))
            ModelState.AddModelError("Form.Email", "Пользователь с таким адресом электронной почты уже существует.");

        if (!ModelState.IsValid)
            return View(await BuildAdminSettingsViewModelAsync(form));

        user.FullName = form.FullName.Trim();
        user.Email = normalizedEmail;
        if (!string.IsNullOrWhiteSpace(form.NewPassword))
            user.PasswordHash = form.NewPassword.Trim();

        await _db.SaveChangesAsync();
        SyncSession(user.FullName, user.Email);

        TempData["Success"] = "Настройки администратора обновлены.";
        return RedirectToAction(nameof(Admin));
    }

    [SessionAuthorize("admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminSiteSettings([Bind(Prefix = "SiteSettingsForm")] AdminSiteSettingsForm form)
    {
        var admin = await GetCurrentUserAsync();
        if (admin is null)
            return RedirectToAction("Login", "Auth");

        if (!ModelState.IsValid)
        {
            var vm = await BuildAdminSettingsViewModelAsync(
                new UpdateAdminSettingsForm { FullName = admin.FullName, Email = admin.Email },
                siteSettingsForm: form);
            return View("Admin", vm);
        }

        SaveSiteSettingsForCurrentUser(form.Theme, form.InterfaceMode);
        TempData["Success"] = "Настройки интерфейса администратора обновлены.";
        return RedirectToAction(nameof(Admin));
    }

    [SessionAuthorize("admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetUserPassword([Bind(Prefix = "PasswordResetForm")] AdminPasswordResetForm form)
    {
        var admin = await GetCurrentUserAsync();
        if (admin is null)
            return RedirectToAction("Login", "Auth");

        if (!ModelState.IsValid)
        {
            var vm = await BuildAdminSettingsViewModelAsync(
                new UpdateAdminSettingsForm { FullName = admin.FullName, Email = admin.Email },
                passwordResetForm: form);
            return View("Admin", vm);
        }

        var normalizedEmail = NormalizeEmail(form.Email);
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email != null && x.Email.Trim().ToLower() == normalizedEmail);
        if (user is null)
        {
            TempData["Error"] = "Пользователь с указанным адресом электронной почты не найден.";
            return RedirectToAction(nameof(Admin));
        }

        user.PasswordHash = form.NewPassword.Trim();
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Пароль пользователя {user.Email} обновлен.";

        return RedirectToAction(nameof(Admin));
    }

    [SessionAuthorize("admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRoleAccess([Bind(Prefix = "RoleAccessForm")] AdminRoleAccessForm form)
    {
        var admin = await GetCurrentUserAsync();
        if (admin is null)
            return RedirectToAction("Login", "Auth");

        var normalizedRole = NormalizeRole(form.Role);
        form.Role = normalizedRole;

        if (!ModelState.IsValid)
        {
            var vm = await BuildAdminSettingsViewModelAsync(
                new UpdateAdminSettingsForm { FullName = admin.FullName, Email = admin.Email },
                roleAccessForm: form);
            return View("Admin", vm);
        }

        var users = await _db.Users.Where(x => x.Role == normalizedRole).ToListAsync();
        if (!form.EnableAccess)
        {
            var currentUserId = HttpContext.Session.GetInt32(AppSession.UserId);
            if (currentUserId.HasValue)
                users = users.Where(x => x.Id != currentUserId.Value).ToList();
        }

        if (users.Count == 0)
        {
            TempData["Error"] = "Нет подходящих учетных записей для выбранного действия.";
            return RedirectToAction(nameof(Admin));
        }

        foreach (var user in users)
            user.IsActive = form.EnableAccess;

        await _db.SaveChangesAsync();

        var roleTitle = normalizedRole switch
        {
            "admin" => "администраторов",
            "teacher" => "преподавателей",
            _ => "учеников"
        };

        TempData["Success"] = form.EnableAccess
            ? $"Доступ включен для роли: {roleTitle}."
            : $"Доступ отключен для роли: {roleTitle}.";

        return RedirectToAction(nameof(Admin));
    }

    [SessionAuthorize("admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncAccounts([Bind(Prefix = "SyncAccountsForm")] AdminSyncAccountsForm form)
    {
        var admin = await GetCurrentUserAsync();
        if (admin is null)
            return RedirectToAction("Login", "Auth");

        if (!ModelState.IsValid)
        {
            var vm = await BuildAdminSettingsViewModelAsync(
                new UpdateAdminSettingsForm { FullName = admin.FullName, Email = admin.Email },
                syncAccountsForm: form);
            return View("Admin", vm);
        }

        if (!form.IncludeTeachers && !form.IncludeStudents)
        {
            TempData["Error"] = "Выберите хотя бы одну категорию для автосоздания учетных записей.";
            return RedirectToAction(nameof(Admin));
        }

        var password = form.DefaultPassword.Trim();
        var knownEmails = new HashSet<string>(
            await _db.Users.AsNoTracking().Select(x => x.Email).ToListAsync(),
            StringComparer.OrdinalIgnoreCase);

        var createdTeachers = 0;
        var createdStudents = 0;
        var skipped = 0;

        if (form.IncludeTeachers)
        {
            var teachers = await _db.Teachers.AsNoTracking().OrderBy(x => x.FullName).ToListAsync();
            foreach (var teacher in teachers)
            {
                var email = NormalizeEmail(teacher.Email);
                if (string.IsNullOrWhiteSpace(email) || knownEmails.Contains(email))
                {
                    skipped++;
                    continue;
                }

                _db.Users.Add(new UserAccount
                {
                    FullName = teacher.FullName,
                    Email = email,
                    PasswordHash = password,
                    Role = "teacher",
                    IsActive = true
                });

                knownEmails.Add(email);
                createdTeachers++;
            }
        }

        if (form.IncludeStudents)
        {
            var students = await _db.Students.AsNoTracking().OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToListAsync();
            foreach (var student in students)
            {
                var email = NormalizeEmail(student.Email);
                if (string.IsNullOrWhiteSpace(email) || knownEmails.Contains(email))
                {
                    skipped++;
                    continue;
                }

                _db.Users.Add(new UserAccount
                {
                    FullName = $"{student.LastName} {student.FirstName} {student.MiddleName}".Trim(),
                    Email = email,
                    PasswordHash = password,
                    Role = "student",
                    IsActive = true
                });

                knownEmails.Add(email);
                createdStudents++;
            }
        }

        if (createdStudents > 0 || createdTeachers > 0)
            await _db.SaveChangesAsync();

        TempData["Success"] = $"Создание завершено: преподаватели {createdTeachers}, ученики {createdStudents}, пропущено {skipped}.";
        return RedirectToAction(nameof(Admin));
    }

    [SessionAuthorize("admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseAllLiveLessons()
    {
        var liveLessons = await _db.Schedules
            .Where(x => x.LiveStatus == AppConstants.LessonLiveStatus.Live)
            .ToListAsync();

        foreach (var lesson in liveLessons)
        {
            lesson.LiveStatus = AppConstants.LessonLiveStatus.Finished;
            lesson.LiveEndedAt = DateTime.UtcNow;
            lesson.LiveStartedAt ??= DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = liveLessons.Count == 0
            ? "Активных онлайн-уроков не найдено."
            : $"Завершено онлайн-уроков: {liveLessons.Count}.";

        return RedirectToAction(nameof(Admin));
    }

    [SessionAuthorize("teacher", "admin")]
    [HttpGet]
    public async Task<IActionResult> Teacher()
    {
        var vm = await BuildTeacherProfileViewModelAsync();
        return View(vm);
    }

    [SessionAuthorize("teacher", "admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Teacher([Bind(Prefix = "Form")] UpdateTeacherProfileForm form)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return RedirectToAction("Login", "Auth");

        var userEmail = NormalizeEmail(user.Email);
        var teacher = await _db.Teachers.FirstOrDefaultAsync(x => x.Email != null && x.Email.Trim().ToLower() == userEmail);
        if (teacher is null)
        {
            TempData["Error"] = "Профиль преподавателя не найден. Обратитесь к администратору.";
            return RedirectToAction(nameof(Teacher));
        }

        var normalizedEmail = NormalizeEmail(form.Email);

        if (await _db.Users.AnyAsync(x => x.Id != user.Id && x.Email != null && x.Email.Trim().ToLower() == normalizedEmail))
            ModelState.AddModelError("Form.Email", "Пользователь с таким адресом электронной почты уже существует.");

        if (await _db.Teachers.AnyAsync(x => x.Id != teacher.Id && x.Email != null && x.Email.Trim().ToLower() == normalizedEmail))
            ModelState.AddModelError("Form.Email", "Преподаватель с таким адресом электронной почты уже существует.");

        if (!ModelState.IsValid)
        {
            var vm = await BuildTeacherProfileViewModelAsync();
            vm.Form = form;
            return View(vm);
        }

        teacher.FullName = form.FullName.Trim();
        teacher.Email = normalizedEmail;
        teacher.Department = form.Department.Trim();
        teacher.PositionName = string.IsNullOrWhiteSpace(form.PositionName) ? null : form.PositionName.Trim();

        user.FullName = teacher.FullName;
        user.Email = normalizedEmail;
        if (!string.IsNullOrWhiteSpace(form.NewPassword))
            user.PasswordHash = form.NewPassword.Trim();

        await _db.SaveChangesAsync();
        SyncSession(user.FullName, user.Email);
        TempData["Success"] = "Профиль преподавателя обновлен.";

        return RedirectToAction(nameof(Teacher));
    }

    [SessionAuthorize("teacher", "admin")]
    [HttpGet]
    public IActionResult TeacherSiteSettings()
    {
        return View(BuildTeacherSiteSettingsViewModel());
    }

    [SessionAuthorize("teacher", "admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TeacherSiteSettings([Bind(Prefix = "Form")] TeacherSiteSettingsForm form)
    {
        if (!ModelState.IsValid)
        {
            return View(BuildTeacherSiteSettingsViewModel(form));
        }

        SaveTeacherSiteSettings(form);
        TempData["Success"] = "Настройки сайта преподавателя обновлены.";
        return RedirectToAction(nameof(TeacherSiteSettings));
    }

    [SessionAuthorize("student", "admin")]
    [HttpGet]
    public async Task<IActionResult> Student()
    {
        var vm = await BuildStudentProfileViewModelAsync();
        return View(vm);
    }

    [SessionAuthorize("student", "admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Student([Bind(Prefix = "Form")] UpdateStudentProfileForm form)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return RedirectToAction("Login", "Auth");

        var userEmail = NormalizeEmail(user.Email);
        var student = await _db.Students.FirstOrDefaultAsync(x => x.Email != null && x.Email.Trim().ToLower() == userEmail);
        if (student is null)
        {
            TempData["Error"] = "Профиль ученика не найден. Обратитесь к администратору.";
            return RedirectToAction(nameof(Student));
        }

        var normalizedEmail = NormalizeEmail(form.Email);

        if (await _db.Users.AnyAsync(x => x.Id != user.Id && x.Email != null && x.Email.Trim().ToLower() == normalizedEmail))
            ModelState.AddModelError("Form.Email", "Пользователь с таким адресом электронной почты уже существует.");

        if (await _db.Students.AnyAsync(x => x.Id != student.Id && x.Email != null && x.Email.Trim().ToLower() == normalizedEmail))
            ModelState.AddModelError("Form.Email", "Ученик с таким адресом электронной почты уже существует.");

        if (!ModelState.IsValid)
        {
            var vm = await BuildStudentProfileViewModelAsync();
            vm.Form = form;
            return View(vm);
        }

        student.LastName = form.LastName.Trim();
        student.FirstName = form.FirstName.Trim();
        student.MiddleName = string.IsNullOrWhiteSpace(form.MiddleName) ? null : form.MiddleName.Trim();
        student.BirthDate = form.BirthDate.Date;
        student.Email = normalizedEmail;
        student.Phone = string.IsNullOrWhiteSpace(form.Phone) ? null : form.Phone.Trim();

        user.FullName = $"{student.LastName} {student.FirstName} {student.MiddleName}".Trim();
        user.Email = normalizedEmail;
        if (!string.IsNullOrWhiteSpace(form.NewPassword))
            user.PasswordHash = form.NewPassword.Trim();

        await _db.SaveChangesAsync();
        SyncSession(user.FullName, user.Email);
        TempData["Success"] = "Профиль ученика обновлен.";

        return RedirectToAction(nameof(Student));
    }

    [SessionAuthorize("student", "admin")]
    [HttpGet]
    public IActionResult StudentSiteSettings()
    {
        return View(BuildStudentSiteSettingsViewModel());
    }

    [SessionAuthorize("student", "admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult StudentSiteSettings([Bind(Prefix = "Form")] StudentSiteSettingsForm form)
    {
        if (!ModelState.IsValid)
        {
            return View(BuildStudentSiteSettingsViewModel(form));
        }

        SaveStudentSiteSettings(form);
        TempData["Success"] = "Настройки сайта ученика обновлены.";
        return RedirectToAction(nameof(StudentSiteSettings));
    }

    private async Task<AdminSettingsViewModel> BuildAdminSettingsViewModelAsync(
        UpdateAdminSettingsForm form,
        AdminSiteSettingsForm? siteSettingsForm = null,
        AdminPasswordResetForm? passwordResetForm = null,
        AdminRoleAccessForm? roleAccessForm = null,
        AdminSyncAccountsForm? syncAccountsForm = null)
    {
        return new AdminSettingsViewModel
        {
            CurrentName = form.FullName,
            CurrentEmail = form.Email,
            TotalUsers = await _db.Users.CountAsync(x => x.Role == "admin" || x.Role == "teacher" || x.Role == "student"),
            ActiveUsers = await _db.Users.CountAsync(x => x.IsActive),
            LiveLessons = await _db.Schedules.CountAsync(x => x.LiveStatus == AppConstants.LessonLiveStatus.Live),
            PendingSubmissions = await _db.LessonSubmissions.CountAsync(x =>
                x.Status == AppConstants.SubmissionStatus.New || x.Status == AppConstants.SubmissionStatus.InReview),
            MissingTeacherAccounts = await _db.Teachers.CountAsync(t =>
                !_db.Users.Any(u => u.Role == "teacher" && u.Email != null && t.Email != null && u.Email.Trim().ToLower() == t.Email.Trim().ToLower())),
            MissingStudentAccounts = await _db.Students.CountAsync(s =>
                !_db.Users.Any(u => u.Role == "student" && u.Email != null && s.Email != null && u.Email.Trim().ToLower() == s.Email.Trim().ToLower())),
            Form = form,
            SiteSettingsForm = siteSettingsForm ?? new AdminSiteSettingsForm
            {
                Theme = GetCurrentUserSiteTheme(),
                InterfaceMode = GetCurrentUserInterfaceMode()
            },
            PasswordResetForm = passwordResetForm ?? new AdminPasswordResetForm(),
            RoleAccessForm = roleAccessForm ?? new AdminRoleAccessForm(),
            SyncAccountsForm = syncAccountsForm ?? new AdminSyncAccountsForm()
        };
    }

    private async Task<TeacherProfileViewModel> BuildTeacherProfileViewModelAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return new TeacherProfileViewModel { ProfileFound = false };

        var userEmail = NormalizeEmail(user.Email);
        var teacher = await _db.Teachers.AsNoTracking().FirstOrDefaultAsync(x => x.Email != null && x.Email.Trim().ToLower() == userEmail);
        if (teacher is null)
            return new TeacherProfileViewModel { ProfileFound = false };

        var today = DateTime.Today;
        var weekEnd = today.AddDays(7);

        return new TeacherProfileViewModel
        {
            ProfileFound = true,
            LessonsThisWeek = await _db.Schedules.CountAsync(s =>
                s.Course.TeacherId == teacher.Id && s.LessonDate >= today && s.LessonDate < weekEnd),
            PendingSubmissions = await _db.LessonSubmissions.CountAsync(s =>
                s.Schedule.Course.TeacherId == teacher.Id &&
                (s.Status == AppConstants.SubmissionStatus.New || s.Status == AppConstants.SubmissionStatus.InReview)),
            Form = new UpdateTeacherProfileForm
            {
                FullName = teacher.FullName,
                Email = teacher.Email,
                Department = teacher.Department,
                PositionName = teacher.PositionName
            }
        };
    }

    private async Task<StudentProfileViewModel> BuildStudentProfileViewModelAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return new StudentProfileViewModel { ProfileFound = false };

        var userEmail = NormalizeEmail(user.Email);
        var student = await _db.Students.AsNoTracking().FirstOrDefaultAsync(x => x.Email != null && x.Email.Trim().ToLower() == userEmail);
        if (student is null)
            return new StudentProfileViewModel { ProfileFound = false };

        return new StudentProfileViewModel
        {
            ProfileFound = true,
            ActiveCourses = await _db.Enrollments.CountAsync(e => e.StudentId == student.Id && e.Status == "active"),
            PendingSubmissions = await _db.LessonSubmissions.CountAsync(s =>
                s.StudentId == student.Id &&
                (s.Status == AppConstants.SubmissionStatus.New ||
                 s.Status == AppConstants.SubmissionStatus.InReview ||
                 s.Status == AppConstants.SubmissionStatus.Revision)),
            Form = new UpdateStudentProfileForm
            {
                LastName = student.LastName,
                FirstName = student.FirstName,
                MiddleName = student.MiddleName,
                BirthDate = student.BirthDate,
                Email = student.Email,
                Phone = student.Phone
            }
        };
    }

    private async Task<UserAccount?> GetCurrentUserAsync()
    {
        var userId = HttpContext.Session.GetInt32(AppSession.UserId);
        if (!userId.HasValue)
            return null;

        return await _db.Users.FirstOrDefaultAsync(x => x.Id == userId.Value);
    }

    private TeacherSiteSettingsViewModel BuildTeacherSiteSettingsViewModel(TeacherSiteSettingsForm? form = null)
    {
        return new TeacherSiteSettingsViewModel
        {
            Form = form ?? new TeacherSiteSettingsForm
            {
                Theme = GetCurrentUserSiteTheme(),
                InterfaceMode = GetCurrentUserInterfaceMode()
            }
        };
    }

    private StudentSiteSettingsViewModel BuildStudentSiteSettingsViewModel(StudentSiteSettingsForm? form = null)
    {
        return new StudentSiteSettingsViewModel
        {
            Form = form ?? new StudentSiteSettingsForm
            {
                Theme = GetCurrentUserSiteTheme(),
                InterfaceMode = GetCurrentUserInterfaceMode()
            }
        };
    }

    private void SaveTeacherSiteSettings(TeacherSiteSettingsForm form)
    {
        SaveSiteSettingsForCurrentUser(form.Theme, form.InterfaceMode);
    }

    private void SaveStudentSiteSettings(StudentSiteSettingsForm form)
    {
        SaveSiteSettingsForCurrentUser(form.Theme, form.InterfaceMode);
    }

    private string GetCurrentUserSiteTheme()
    {
        var userId = HttpContext.Session.GetInt32(AppSession.UserId);
        if (userId.HasValue)
        {
            var perUserCookieKey = GetPerUserThemeCookieKey(userId.Value);
            if (Request.Cookies.TryGetValue(perUserCookieKey, out var perUserTheme))
                return NormalizeTheme(perUserTheme);
        }

        return "light";
    }

    private string GetCurrentUserInterfaceMode()
    {
        var userId = HttpContext.Session.GetInt32(AppSession.UserId);
        if (userId.HasValue)
        {
            var perUserCookieKey = GetPerUserInterfaceModeCookieKey(userId.Value);
            if (Request.Cookies.TryGetValue(perUserCookieKey, out var perUserMode))
                return NormalizeInterfaceMode(perUserMode);
        }

        return "normal";
    }

    private void SaveSiteSettingsForCurrentUser(
        string? theme,
        string? interfaceMode)
    {
        var normalizedTheme = NormalizeTheme(theme);
        var normalizedInterfaceMode = NormalizeInterfaceMode(interfaceMode);

        var userId = HttpContext.Session.GetInt32(AppSession.UserId);
        if (userId.HasValue)
        {
            SetCookie(GetPerUserThemeCookieKey(userId.Value), normalizedTheme);
            SetCookie(GetPerUserInterfaceModeCookieKey(userId.Value), normalizedInterfaceMode);
        }

        // Оставляем тему и в общем cookie для страницы входа.
        SetCookie(SiteThemeCookie, normalizedTheme);
        SetCookie(InterfaceModeCookie, normalizedInterfaceMode);
    }

    private void SetCookie(string key, string value)
    {
        Response.Cookies.Append(
            key,
            value,
            new CookieOptions
            {
                HttpOnly = false,
                IsEssential = true,
                Path = "/",
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
    }

    private static string NormalizeTheme(string? value)
        => string.Equals(value?.Trim(), "dark", StringComparison.OrdinalIgnoreCase) ? "dark" : "light";

    private static string GetPerUserThemeCookieKey(int userId)
        => $"{SiteThemePerUserPrefix}{userId}";

    private static string NormalizeInterfaceMode(string? value)
        => string.Equals(value?.Trim(), "compact", StringComparison.OrdinalIgnoreCase) ? "compact" : "normal";

    private static string GetPerUserInterfaceModeCookieKey(int userId)
        => $"{InterfaceModePerUserPrefix}{userId}";

    private static string NormalizeEmail(string? email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();

    private static string NormalizeRole(string? role)
        => (role ?? string.Empty).Trim().ToLowerInvariant();
    private void SyncSession(string fullName, string email)
    {
        HttpContext.Session.SetString(AppSession.UserName, fullName);
        HttpContext.Session.SetString(AppSession.UserEmail, NormalizeEmail(email));
    }
}
