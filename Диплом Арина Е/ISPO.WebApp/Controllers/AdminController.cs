
using System.Text;
using ISPO.WebApp.Data;
using ISPO.WebApp.Filters;
using ISPO.WebApp.Infrastructure;
using ISPO.WebApp.Models;
using ISPO.WebApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ISPO.WebApp.Controllers;

[SessionAuthorize("admin")]
public class AdminController : Controller
{
    private readonly DiplomIspoDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AdminController> _logger;
    private static readonly string[] AllowedNewsImageExtensions = [".jpg", ".jpeg", ".png", ".jfif", ".webp"];
    private const long MaxNewsImageSizeBytes = 25L * 1024 * 1024;

    public AdminController(DiplomIspoDbContext db, IWebHostEnvironment environment, ILogger<AdminController> logger)
    {
        _db = db;
        _environment = environment;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        return View(await BuildAdminDashboardSafeAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(200L * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 200L * 1024 * 1024)]
    public async Task<IActionResult> CreateNews([Bind(Prefix = "NewsForm")] CreateNewsViewModel? form, IFormFile? newsImage)
    {
        form ??= new CreateNewsViewModel();
        form.Body = (form.Body ?? string.Empty).Trim();
        var imageFile = newsImage;

        AppendNewsUploadLog($"START: bodyLen={form.Body.Length}; hasFile={imageFile is not null}; fileName={imageFile?.FileName}; size={imageFile?.Length}; contentType={imageFile?.ContentType}");

        if (string.IsNullOrWhiteSpace(form.Body))
            ModelState.AddModelError("NewsForm.Body", "Введите текст новости.");

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildAdminDashboardSafeAsync(form));
        }

        try
        {
            string? imageUrl = null;
            if (imageFile is not null && imageFile.Length > 0)
            {
                if (imageFile.Length > MaxNewsImageSizeBytes)
                    ModelState.AddModelError("NewsImage", "Размер изображения не должен превышать 25 МБ.");

                var fileNameExtension = (Path.GetExtension(imageFile.FileName) ?? string.Empty).Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(fileNameExtension) && !AllowedNewsImageExtensions.Contains(fileNameExtension))
                    ModelState.AddModelError("NewsImage", "Поддерживаются изображения JPG, PNG и WEBP.");

                var detectedExtension = await DetectNewsImageExtensionAsync(imageFile);
                if (string.IsNullOrWhiteSpace(detectedExtension))
                    ModelState.AddModelError("NewsImage", "Файл не распознан как изображение JPG, PNG или WEBP.");

                if (!ModelState.IsValid)
                {
                    return View("Index", await BuildAdminDashboardSafeAsync(form));
                }

                try
                {
                    imageUrl = await SaveNewsImageAsync(imageFile, detectedExtension!);
                    AppendNewsUploadLog($"SAVED_FILE: url={imageUrl}");
                }
                catch (Exception ex)
                {
                    imageUrl = null;
                    AppendNewsUploadLog($"SAVE_FILE_ERROR: {ex.GetType().Name}: {ex.Message}");
                    _logger.LogError(ex, "Не удалось сохранить изображение новости. Новость будет опубликована без фото.");
                    TempData["Error"] = "Новость опубликована без изображения: не удалось сохранить файл.";
                }
            }

            _db.NewsPosts.Add(new NewsPost
            {
                Body = form.Body,
                ImageUrl = imageUrl,
                CreatedBy = HttpContext.Session.GetString(AppSession.UserName) ?? "Администратор",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            AppendNewsUploadLog("DB_SAVED: success");
            TempData["Success"] = "Новость опубликована.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            AppendNewsUploadLog($"ERROR: {ex.GetType().Name}: {ex.Message}");
            _logger.LogError(ex, "Ошибка публикации новости. Пользователь: {UserId}", HttpContext.Session.GetInt32(AppSession.UserId));
            TempData["Error"] = "Не удалось опубликовать новость. Попробуйте снова или обратитесь к администратору.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteNews(int id)
    {
        try
        {
            var news = await _db.NewsPosts.FindAsync(id);
            if (news is null)
                return RedirectToAction(nameof(Index));

            if (!string.IsNullOrWhiteSpace(news.ImageUrl))
            {
                var fullPath = ResolveNewsImageFullPath(news.ImageUrl);
                if (!string.IsNullOrWhiteSpace(fullPath) && System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            _db.NewsPosts.Remove(news);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Новость удалена.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка удаления новости Id={NewsId}", id);
            TempData["Error"] = "Не удалось удалить новость. Попробуйте позже.";
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> Users(string? role = null)
    {
        var normalizedRole = role?.Trim().ToLowerInvariant();
        if (normalizedRole is not ("admin" or "teacher" or "student"))
            normalizedRole = null;

        var query = _db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(normalizedRole))
            query = query.Where(u => u.Role == normalizedRole);

        var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
        var studentsByEmail = await _db.Students
            .AsNoTracking()
            .Where(s => s.Email != null && s.GroupId != 0)
            .Select(s => new { Email = s.Email!.Trim().ToLower(), s.GroupId })
            .ToListAsync();

        var userGroupIds = new Dictionary<int, int?>();
        foreach (var user in users)
        {
            var groupId = studentsByEmail
                .FirstOrDefault(s => string.Equals(s.Email, user.Email?.Trim().ToLower(), StringComparison.Ordinal))?.GroupId;
            userGroupIds[user.Id] = groupId;
        }

        var vm = new UserManagementViewModel
        {
            UserFullName = HttpContext.Session.GetString(AppSession.UserName) ?? "Администратор",
            RoleFilter = normalizedRole,
            Users = users,
            Groups = await _db.StudentGroups.AsNoTracking().OrderBy(g => g.GroupCode).ToListAsync(),
            UserGroupIds = userGroupIds,
            Form = new CreateUserViewModel()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser([Bind(Prefix = "Form")] CreateUserViewModel? form)
    {
        form ??= new CreateUserViewModel();

        form.FullName = (form.FullName ?? string.Empty).Trim();
        form.Email = (form.Email ?? string.Empty).Trim();
        form.Role = (form.Role ?? string.Empty).Trim();
        form.Password = (form.Password ?? string.Empty).Trim();

        var fullName = form.FullName;
        var email = form.Email.ToLowerInvariant();
        var role = form.Role.ToLowerInvariant();
        var password = form.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(password))
            ModelState.AddModelError("Form.Password", "Введите пароль.");

        if (!ModelState.IsValid)
            return View("Users", await BuildUsersViewModelForValidationAsync(form));

        if (await _db.Users.AnyAsync(u => u.Email != null && u.Email.Trim().ToLower() == email))
            ModelState.AddModelError("Form.Email", "Пользователь с таким адресом электронной почты уже существует.");

        if (role is not ("admin" or "teacher" or "student"))
            ModelState.AddModelError("Form.Role", "Недопустимая роль.");

        if (role == "student")
        {
            if (!form.GroupId.HasValue || form.GroupId.Value <= 0)
            {
                ModelState.AddModelError("Form.GroupId", "Для ученика необходимо выбрать группу.");
            }
            else
            {
                var groupExists = await _db.StudentGroups
                    .AsNoTracking()
                    .AnyAsync(g => g.Id == form.GroupId.Value);

                if (!groupExists)
                    ModelState.AddModelError("Form.GroupId", "Выбранная группа не найдена.");
            }
        }

        if (!ModelState.IsValid)
            return View("Users", await BuildUsersViewModelForValidationAsync(form));

        if (role == "teacher")
        {
            var teacherProfile = await _db.Teachers
                .FirstOrDefaultAsync(t => t.Email != null && t.Email.Trim().ToLower() == email);

            if (teacherProfile is null)
            {
                _db.Teachers.Add(new Teacher
                {
                    FullName = fullName,
                    Email = email,
                    Department = "Общее отделение",
                    PositionName = "Преподаватель"
                });
            }
            else
            {
                teacherProfile.FullName = fullName;
                teacherProfile.Email = email;
            }
        }

        if (role == "student" && form.GroupId.HasValue)
        {
            var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var lastName = nameParts.Length > 0 ? nameParts[0] : "Ученик";
            var firstName = nameParts.Length > 1 ? nameParts[1] : "Новый";
            var middleName = nameParts.Length > 2 ? string.Join(' ', nameParts.Skip(2)) : null;

            var studentProfile = await _db.Students
                .FirstOrDefaultAsync(s => s.Email != null && s.Email.Trim().ToLower() == email);

            if (studentProfile is null)
            {
                _db.Students.Add(new Student
                {
                    LastName = lastName,
                    FirstName = firstName,
                    MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName,
                    BirthDate = DateTime.Today.AddYears(-18),
                    Email = email,
                    GroupId = form.GroupId.Value
                });
            }
            else
            {
                studentProfile.LastName = lastName;
                studentProfile.FirstName = firstName;
                studentProfile.MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName;
                studentProfile.Email = email;
                studentProfile.GroupId = form.GroupId.Value;
            }
        }

        _db.Users.Add(new UserAccount
        {
            FullName = fullName,
            Email = email,
            PasswordHash = password,
            Role = role,
            IsActive = true
        });

        await _db.SaveChangesAsync();

        TempData["Success"] = role switch
        {
            "teacher" => "Пользователь создан и профиль преподавателя добавлен.",
            "student" => "Пользователь создан и профиль ученика добавлен.",
            _ => "Пользователь создан."
        };

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUser([Bind(Prefix = "Form")] CreateUserViewModel? form)
    {
        form ??= new CreateUserViewModel();
        form.FullName = (form.FullName ?? string.Empty).Trim();
        form.Email = (form.Email ?? string.Empty).Trim();
        form.Role = (form.Role ?? string.Empty).Trim();
        form.Password = form.Password?.Trim();

        var role = form.Role.ToLowerInvariant();
        var email = form.Email.ToLowerInvariant();

        if (!form.UserId.HasValue || form.UserId.Value <= 0)
            ModelState.AddModelError("Form.UserId", "Не выбран пользователь для редактирования.");

        if (role is not ("admin" or "teacher" or "student"))
            ModelState.AddModelError("Form.Role", "Недопустимая роль.");

        if (role == "student")
        {
            if (!form.GroupId.HasValue || form.GroupId.Value <= 0)
            {
                ModelState.AddModelError("Form.GroupId", "Для ученика необходимо выбрать группу.");
            }
        }

        if (!ModelState.IsValid)
            return View("Users", await BuildUsersViewModelForValidationAsync(form));

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == form.UserId.Value);
        if (user is null)
        {
            TempData["Error"] = "Пользователь не найден.";
            return RedirectToAction(nameof(Users));
        }

        if (await _db.Users.AnyAsync(u => u.Id != user.Id && u.Email != null && u.Email.Trim().ToLower() == email))
            ModelState.AddModelError("Form.Email", "Пользователь с таким адресом электронной почты уже существует.");

        if (!ModelState.IsValid)
            return View("Users", await BuildUsersViewModelForValidationAsync(form));

        user.FullName = form.FullName;
        user.Email = email;
        user.Role = role;
        if (!string.IsNullOrWhiteSpace(form.Password))
            user.PasswordHash = form.Password;

        if (role == "teacher")
        {
            var teacherProfile = await _db.Teachers.FirstOrDefaultAsync(t => t.Email != null && t.Email.Trim().ToLower() == email);
            if (teacherProfile is null)
            {
                _db.Teachers.Add(new Teacher
                {
                    FullName = form.FullName,
                    Email = email,
                    Department = "Общее отделение",
                    PositionName = "Преподаватель"
                });
            }
            else
            {
                teacherProfile.FullName = form.FullName;
                teacherProfile.Email = email;
            }
        }

        if (role == "student" && form.GroupId.HasValue)
        {
            var nameParts = form.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var lastName = nameParts.Length > 0 ? nameParts[0] : "Ученик";
            var firstName = nameParts.Length > 1 ? nameParts[1] : "Новый";
            var middleName = nameParts.Length > 2 ? string.Join(' ', nameParts.Skip(2)) : null;

            var studentProfile = await _db.Students.FirstOrDefaultAsync(s => s.Email != null && s.Email.Trim().ToLower() == email);
            if (studentProfile is null)
            {
                _db.Students.Add(new Student
                {
                    LastName = lastName,
                    FirstName = firstName,
                    MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName,
                    BirthDate = DateTime.Today.AddYears(-18),
                    Email = email,
                    GroupId = form.GroupId.Value
                });
            }
            else
            {
                studentProfile.LastName = lastName;
                studentProfile.FirstName = firstName;
                studentProfile.MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName;
                studentProfile.Email = email;
                studentProfile.GroupId = form.GroupId.Value;
            }
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Данные пользователя обновлены.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null)
            return RedirectToAction(nameof(Users));

        var currentUserId = HttpContext.Session.GetInt32(AppSession.UserId);
        if (currentUserId == user.Id && user.IsActive)
        {
            TempData["Error"] = "Нельзя отключить свою текущую учетную запись.";
            return RedirectToAction(nameof(Users));
        }

        user.IsActive = !user.IsActive;
        await _db.SaveChangesAsync();

        TempData["Success"] = user.IsActive ? "Учетная запись активирована." : "Учетная запись деактивирована.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null)
            return RedirectToAction(nameof(Users));

        var currentUserId = HttpContext.Session.GetInt32(AppSession.UserId);
        if (currentUserId == user.Id)
        {
            TempData["Error"] = "Нельзя удалить свою учетную запись.";
            return RedirectToAction(nameof(Users));
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Пользователь удален.";
        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> Groups()
    {
        var vm = new AdminGroupsViewModel
        {
            Groups = await _db.StudentGroups
                .AsNoTracking()
                .OrderBy(g => g.GroupCode)
                .ToListAsync(),
            Form = new CreateGroupViewModel()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGroup([Bind(Prefix = "Form")] CreateGroupViewModel form)
    {
        if (await _db.StudentGroups.AnyAsync(g => g.GroupCode == form.GroupCode))
            ModelState.AddModelError("Form.GroupCode", "Группа с таким кодом уже существует.");

        if (!ModelState.IsValid)
        {
            var vm = new AdminGroupsViewModel
            {
                Groups = await _db.StudentGroups.AsNoTracking().OrderBy(g => g.GroupCode).ToListAsync(),
                Form = form
            };
            return View("Groups", vm);
        }

        _db.StudentGroups.Add(new StudentGroup
        {
            GroupCode = form.GroupCode.Trim(),
            Name = form.Name.Trim(),
            Specialization = form.Specialization.Trim(),
            StartYear = form.StartYear
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = "Группа добавлена.";
        return RedirectToAction(nameof(Groups));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateGroup(int id, string groupCode, string name, string specialization, int startYear)
    {
        var group = await _db.StudentGroups.FindAsync(id);
        if (group is null)
            return RedirectToAction(nameof(Groups));

        if (await _db.StudentGroups.AnyAsync(g => g.Id != id && g.GroupCode == groupCode.Trim()))
        {
            TempData["Error"] = "Код группы должен быть уникальным.";
            return RedirectToAction(nameof(Groups));
        }

        group.GroupCode = groupCode.Trim();
        group.Name = name.Trim();
        group.Specialization = specialization.Trim();
        group.StartYear = startYear;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Группа обновлена.";
        return RedirectToAction(nameof(Groups));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        var group = await _db.StudentGroups.FindAsync(id);
        if (group is null)
            return RedirectToAction(nameof(Groups));

        var hasStudents = await _db.Students.AnyAsync(s => s.GroupId == id);
        var hasSchedules = await _db.Schedules.AnyAsync(s => s.GroupId == id);
        if (hasStudents || hasSchedules)
        {
            TempData["Error"] = "Нельзя удалить группу: есть связанные студенты или занятия.";
            return RedirectToAction(nameof(Groups));
        }

        _db.StudentGroups.Remove(group);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Группа удалена.";
        return RedirectToAction(nameof(Groups));
    }
    public async Task<IActionResult> Teachers()
    {
        var vm = new AdminTeachersViewModel
        {
            Teachers = await _db.Teachers
                .AsNoTracking()
                .OrderBy(t => t.FullName)
                .ToListAsync(),
            Form = new CreateTeacherViewModel()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTeacher([Bind(Prefix = "Form")] CreateTeacherViewModel form)
    {
        var email = form.Email.Trim().ToLowerInvariant();

        if (await _db.Teachers.AnyAsync(t => t.Email != null && t.Email.Trim().ToLower() == email))
            ModelState.AddModelError("Form.Email", "Преподаватель с таким адресом электронной почты уже существует.");

        if (await _db.Users.AnyAsync(u => u.Email != null && u.Email.Trim().ToLower() == email))
            ModelState.AddModelError("Form.Email", "Пользователь с таким адресом электронной почты уже существует.");

        if (!ModelState.IsValid)
        {
            var vm = new AdminTeachersViewModel
            {
                Teachers = await _db.Teachers.AsNoTracking().OrderBy(t => t.FullName).ToListAsync(),
                Form = form
            };
            return View("Teachers", vm);
        }

        var teacher = new Teacher
        {
            FullName = form.FullName.Trim(),
            Email = email,
            Department = form.Department.Trim(),
            PositionName = string.IsNullOrWhiteSpace(form.PositionName) ? null : form.PositionName.Trim()
        };

        _db.Teachers.Add(teacher);
        _db.Users.Add(new UserAccount
        {
            FullName = teacher.FullName,
            Email = email,
            PasswordHash = form.InitialPassword.Trim(),
            Role = "teacher",
            IsActive = true
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = "Преподаватель добавлен, учетная запись для входа создана.";
        return RedirectToAction(nameof(Teachers));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTeacher(int id, string fullName, string email, string department, string? positionName)
    {
        var teacher = await _db.Teachers.FindAsync(id);
        if (teacher is null)
            return RedirectToAction(nameof(Teachers));

        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (await _db.Teachers.AnyAsync(t => t.Id != id && t.Email != null && t.Email.Trim().ToLower() == normalizedEmail))
        {
            TempData["Error"] = "Адрес электронной почты преподавателя должен быть уникальным.";
            return RedirectToAction(nameof(Teachers));
        }

        var linkedUser = await _db.Users.FirstOrDefaultAsync(u => u.Role == "teacher" && u.Email != null && teacher.Email != null && u.Email.Trim().ToLower() == teacher.Email.Trim().ToLower());
        if (linkedUser is not null && await _db.Users.AnyAsync(u => u.Id != linkedUser.Id && u.Email != null && u.Email.Trim().ToLower() == normalizedEmail))
        {
            TempData["Error"] = "Этот адрес электронной почты уже используется другой учетной записью.";
            return RedirectToAction(nameof(Teachers));
        }

        teacher.FullName = fullName.Trim();
        teacher.Email = normalizedEmail;
        teacher.Department = department.Trim();
        teacher.PositionName = string.IsNullOrWhiteSpace(positionName) ? null : positionName.Trim();

        if (linkedUser is not null)
        {
            linkedUser.FullName = teacher.FullName;
            linkedUser.Email = normalizedEmail;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Данные преподавателя обновлены.";
        return RedirectToAction(nameof(Teachers));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTeacher(int id)
    {
        var teacher = await _db.Teachers.FindAsync(id);
        if (teacher is null)
            return RedirectToAction(nameof(Teachers));

        var hasCourses = await _db.Courses.AnyAsync(c => c.TeacherId == id);
        if (hasCourses)
        {
            TempData["Error"] = "Нельзя удалить преподавателя: есть связанные курсы.";
            return RedirectToAction(nameof(Teachers));
        }

        var linkedUser = await _db.Users.FirstOrDefaultAsync(u => u.Role == "teacher" && u.Email != null && teacher.Email != null && u.Email.Trim().ToLower() == teacher.Email.Trim().ToLower());
        if (linkedUser is not null)
            _db.Users.Remove(linkedUser);

        _db.Teachers.Remove(teacher);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Преподаватель и учетная запись удалены.";
        return RedirectToAction(nameof(Teachers));
    }

    public async Task<IActionResult> Courses()
    {
        var vm = await BuildCoursesViewModelAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCourse([Bind(Prefix = "Form")] CreateCourseViewModel form)
    {
        form.CourseCode = (form.CourseCode ?? string.Empty).Trim();
        form.Title = (form.Title ?? string.Empty).Trim();
        form.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();

        var normalizedCode = form.CourseCode.ToLowerInvariant();
        if (await _db.Courses.AnyAsync(c => c.CourseCode.ToLower() == normalizedCode))
            ModelState.AddModelError("Form.CourseCode", "Курс с таким кодом уже существует.");

        var categoryExists = await _db.CourseCategories
            .AsNoTracking()
            .AnyAsync(c => c.Id == form.CategoryId);
        if (!categoryExists)
            ModelState.AddModelError("Form.CategoryId", "Выберите корректную категорию.");

        var teacherExists = await _db.Teachers
            .AsNoTracking()
            .AnyAsync(t => t.Id == form.TeacherId);
        if (!teacherExists)
            ModelState.AddModelError("Form.TeacherId", "Выберите корректного преподавателя.");

        if (!ModelState.IsValid)
            return View("Courses", await BuildCoursesViewModelAsync(form));

        _db.Courses.Add(new Course
        {
            CourseCode = form.CourseCode,
            Title = form.Title,
            Description = form.Description,
            DurationHours = form.DurationHours,
            CategoryId = form.CategoryId,
            TeacherId = form.TeacherId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = "Курс добавлен.";
        return RedirectToAction(nameof(Courses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCourse(
        int id,
        string courseCode,
        string title,
        string? description,
        int durationHours,
        int categoryId,
        int teacherId,
        bool isActive)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course is null)
            return RedirectToAction(nameof(Courses));

        courseCode = (courseCode ?? string.Empty).Trim();
        title = (title ?? string.Empty).Trim();
        var normalizedCode = courseCode.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            TempData["Error"] = "Код курса не может быть пустым.";
            return RedirectToAction(nameof(Courses));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["Error"] = "Название курса не может быть пустым.";
            return RedirectToAction(nameof(Courses));
        }

        if (await _db.Courses.AnyAsync(c => c.Id != id && c.CourseCode.ToLower() == normalizedCode))
        {
            TempData["Error"] = "Код курса должен быть уникальным.";
            return RedirectToAction(nameof(Courses));
        }

        if (durationHours < 1 || durationHours > 1000)
        {
            TempData["Error"] = "Количество часов должно быть в диапазоне 1-1000.";
            return RedirectToAction(nameof(Courses));
        }

        var categoryExists = await _db.CourseCategories
            .AsNoTracking()
            .AnyAsync(c => c.Id == categoryId);
        if (!categoryExists)
        {
            TempData["Error"] = "Выбранная категория курса не найдена.";
            return RedirectToAction(nameof(Courses));
        }

        var teacherExists = await _db.Teachers
            .AsNoTracking()
            .AnyAsync(t => t.Id == teacherId);
        if (!teacherExists)
        {
            TempData["Error"] = "Выбранный преподаватель не найден.";
            return RedirectToAction(nameof(Courses));
        }

        course.CourseCode = courseCode;
        course.Title = title;
        course.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        course.DurationHours = durationHours;
        course.CategoryId = categoryId;
        course.TeacherId = teacherId;
        course.IsActive = isActive;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Курс обновлен.";
        return RedirectToAction(nameof(Courses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course is null)
            return RedirectToAction(nameof(Courses));

        var hasEnrollments = await _db.Enrollments.AnyAsync(e => e.CourseId == id);
        var hasSchedules = await _db.Schedules.AnyAsync(s => s.CourseId == id);
        var hasMaterials = await _db.CourseMaterials.AnyAsync(m => m.CourseId == id);

        if (hasEnrollments || hasSchedules || hasMaterials)
        {
            TempData["Error"] = "Нельзя удалить курс: есть связанные зачисления, занятия или материалы.";
            return RedirectToAction(nameof(Courses));
        }

        _db.Courses.Remove(course);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Курс удален.";
        return RedirectToAction(nameof(Courses));
    }

    public async Task<IActionResult> Schedule()
    {
        var vm = await BuildScheduleViewModelAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSchedule([Bind(Prefix = "Form")] CreateScheduleViewModel form)
    {
        ValidateScheduleForm(form);
        if (!ModelState.IsValid)
            return View("Schedule", await BuildScheduleViewModelAsync(form));

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
        TempData["Success"] = "Занятие добавлено в расписание.";
        return RedirectToAction(nameof(Schedule));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSchedule(
        int id,
        int courseId,
        int groupId,
        DateTime lessonDate,
        TimeSpan startTime,
        TimeSpan endTime,
        string? room,
        string? lessonTopic,
        string? conferenceUrl,
        string liveStatus)
    {
        var schedule = await _db.Schedules.FindAsync(id);
        if (schedule is null)
            return RedirectToAction(nameof(Schedule));

        if (endTime <= startTime)
        {
            TempData["Error"] = "Время окончания должно быть позже времени начала.";
            return RedirectToAction(nameof(Schedule));
        }

        if (!string.IsNullOrWhiteSpace(conferenceUrl) &&
            !conferenceUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Ссылка на конференцию должна начинаться с https://";
            return RedirectToAction(nameof(Schedule));
        }

        var normalizedStatus = liveStatus.Trim().ToLowerInvariant();
        if (!AppConstants.LessonLiveStatus.All.Contains(normalizedStatus))
            normalizedStatus = AppConstants.LessonLiveStatus.Planned;

        schedule.CourseId = courseId;
        schedule.GroupId = groupId;
        schedule.LessonDate = lessonDate.Date;
        schedule.StartTime = startTime;
        schedule.EndTime = endTime;
        schedule.Room = string.IsNullOrWhiteSpace(room) ? null : room.Trim();
        schedule.LessonTopic = string.IsNullOrWhiteSpace(lessonTopic) ? null : lessonTopic.Trim();
        schedule.ConferenceUrl = string.IsNullOrWhiteSpace(conferenceUrl) ? null : conferenceUrl.Trim();
        schedule.LiveStatus = normalizedStatus;
        if (normalizedStatus == AppConstants.LessonLiveStatus.Planned)
        {
            schedule.LiveStartedAt = null;
            schedule.LiveEndedAt = null;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Занятие обновлено.";
        return RedirectToAction(nameof(Schedule));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSchedule(int id)
    {
        var schedule = await _db.Schedules.FindAsync(id);
        if (schedule is null)
            return RedirectToAction(nameof(Schedule));

        _db.Schedules.Remove(schedule);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Занятие удалено.";
        return RedirectToAction(nameof(Schedule));
    }

    public async Task<IActionResult> Enrollments(string? status = null)
    {
        var normalized = status?.Trim().ToLowerInvariant();
        if (normalized is not ("active" or "completed" or "cancelled"))
            normalized = null;

        var vm = await BuildEnrollmentsViewModelAsync(new CreateEnrollmentViewModel(), normalized);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEnrollment([Bind(Prefix = "Form")] CreateEnrollmentViewModel form)
    {
        if (await _db.Enrollments.AnyAsync(e => e.StudentId == form.StudentId))
            ModelState.AddModelError("Form.StudentId", "Этот студент уже зачислен. Для него доступно только редактирование текущего зачисления.");

        if (await _db.Enrollments.AnyAsync(e => e.StudentId == form.StudentId && e.CourseId == form.CourseId))
            ModelState.AddModelError("Form.CourseId", "Этот студент уже зачислен на выбранный курс.");

        if (!ModelState.IsValid)
            return View("Enrollments", await BuildEnrollmentsViewModelAsync(form, null));

        _db.Enrollments.Add(new Enrollment
        {
            StudentId = form.StudentId,
            CourseId = form.CourseId,
            Status = form.Status.Trim().ToLowerInvariant()
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = "Зачисление добавлено.";
        return RedirectToAction(nameof(Enrollments));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEnrollmentStatus(int id, string status)
    {
        var enrollment = await _db.Enrollments.FindAsync(id);
        if (enrollment is null)
            return RedirectToAction(nameof(Enrollments));

        var normalized = status.Trim().ToLowerInvariant();
        if (normalized is not ("active" or "completed" or "cancelled"))
        {
            TempData["Error"] = "Недопустимый статус зачисления.";
            return RedirectToAction(nameof(Enrollments));
        }

        enrollment.Status = normalized;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Статус зачисления обновлен.";
        return RedirectToAction(nameof(Enrollments));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEnrollment(int id)
    {
        var enrollment = await _db.Enrollments.FindAsync(id);
        if (enrollment is null)
            return RedirectToAction(nameof(Enrollments));

        _db.Enrollments.Remove(enrollment);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Зачисление удалено.";
        return RedirectToAction(nameof(Enrollments));
    }
    [HttpGet("/Admin/Reports/Performance")]
    public async Task<IActionResult> ReportsPerformance(DateTime? dateFrom, DateTime? dateTo, int? groupId, int? courseId, string? format)
    {
        var vm = await BuildPerformanceReportAsync(dateFrom, dateTo, groupId, courseId);
        return RenderReport(vm, format, "performance_report.csv");
    }

    [HttpGet("/Admin/Reports/Attendance")]
    public async Task<IActionResult> ReportsAttendance(DateTime? dateFrom, DateTime? dateTo, int? groupId, int? courseId, string? format)
    {
        var vm = await BuildAttendanceReportAsync(dateFrom, dateTo, groupId, courseId);
        return RenderReport(vm, format, "attendance_report.csv");
    }

    [HttpGet("/Admin/Reports/CourseActivity")]
    public async Task<IActionResult> ReportsCourseActivity(DateTime? dateFrom, DateTime? dateTo, int? groupId, int? courseId, string? format)
    {
        var vm = await BuildCourseActivityReportAsync(dateFrom, dateTo, groupId, courseId);
        return RenderReport(vm, format, "course_activity_report.csv");
    }

    [HttpGet("/Admin/Reports/TeacherLoad")]
    public async Task<IActionResult> ReportsTeacherLoad(DateTime? dateFrom, DateTime? dateTo, int? groupId, int? courseId, string? format)
    {
        var vm = await BuildTeacherLoadReportAsync(dateFrom, dateTo, groupId, courseId);
        return RenderReport(vm, format, "teacher_load_report.csv");
    }

    [HttpGet("/Admin/Reports/GroupProgress")]
    public async Task<IActionResult> ReportsGroupProgress(DateTime? dateFrom, DateTime? dateTo, int? groupId, int? courseId, string? format)
    {
        var vm = await BuildGroupProgressReportAsync(dateFrom, dateTo, groupId, courseId);
        return RenderReport(vm, format, "group_progress_report.csv");
    }

    private async Task<AdminDashboardViewModel> BuildAdminDashboardSafeAsync(CreateNewsViewModel? newsForm = null)
    {
        try
        {
            return await BuildAdminDashboardViewModelAsync(newsForm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Критическая ошибка построения панели администратора.");
            TempData["Error"] ??= "Панель временно недоступна. Обновите страницу.";

            return new AdminDashboardViewModel
            {
                UserFullName = HttpContext.Session.GetString(AppSession.UserName) ?? "Администратор",
                NewsForm = newsForm ?? new CreateNewsViewModel(),
                TopCourses = [],
                News = []
            };
        }
    }

    private async Task<AdminDashboardViewModel> BuildAdminDashboardViewModelAsync(CreateNewsViewModel? newsForm = null)
    {
        var today = DateTime.Today;
        var weekEnd = today.AddDays(7);
        List<NewsCardVm> newsFeed;
        List<CourseLoadVm> topCourses;
        var usersCount = 0;
        var activeUsersCount = 0;
        var studentsCount = 0;
        var teachersCount = 0;
        var groupsCount = 0;
        var coursesCount = 0;
        var lessonsThisWeek = 0;
        var liveLessonsCount = 0;
        var submissionsPendingCount = 0;

        try
        {
            newsFeed = await _db.NewsPosts
                .AsNoTracking()
                .OrderByDescending(n => n.CreatedAt)
                .Take(12)
                .Select(n => new NewsCardVm
                {
                    Id = n.Id,
                    Body = n.Body,
                    ImageUrl = n.ImageUrl,
                    CreatedBy = n.CreatedBy,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка чтения ленты новостей для панели администратора.");
            TempData["Error"] ??= "Лента новостей временно недоступна.";
            newsFeed = [];
        }

        try
        {
            usersCount = await _db.Users.CountAsync();
            activeUsersCount = await _db.Users.CountAsync(u => u.IsActive);
            studentsCount = await _db.Students.CountAsync();
            teachersCount = await _db.Teachers.CountAsync();
            groupsCount = await _db.StudentGroups.CountAsync();
            coursesCount = await _db.Courses.CountAsync();
            lessonsThisWeek = await _db.Schedules.CountAsync(s => s.LessonDate >= today && s.LessonDate < weekEnd);
            liveLessonsCount = await _db.Schedules.CountAsync(s => s.LiveStatus == AppConstants.LessonLiveStatus.Live);
            submissionsPendingCount = await _db.LessonSubmissions.CountAsync(s =>
                s.Status == AppConstants.SubmissionStatus.New || s.Status == AppConstants.SubmissionStatus.InReview);

            topCourses = await _db.Courses
                .AsNoTracking()
                .Include(c => c.Teacher)
                .Include(c => c.Enrollments)
                .OrderByDescending(c => c.Enrollments.Count)
                .ThenBy(c => c.Title)
                .Take(6)
                .Select(c => new CourseLoadVm
                {
                    CourseCode = c.CourseCode,
                    CourseTitle = c.Title,
                    TeacherName = c.Teacher.FullName,
                    StudentsEnrolled = c.Enrollments.Count
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка загрузки статистики панели администратора.");
            TempData["Error"] ??= "Часть статистики временно недоступна.";
            topCourses = [];
        }

        return new AdminDashboardViewModel
        {
            UserFullName = HttpContext.Session.GetString(AppSession.UserName) ?? "Администратор",
            UsersCount = usersCount,
            ActiveUsersCount = activeUsersCount,
            StudentsCount = studentsCount,
            TeachersCount = teachersCount,
            GroupsCount = groupsCount,
            CoursesCount = coursesCount,
            LessonsThisWeek = lessonsThisWeek,
            LiveLessonsCount = liveLessonsCount,
            SubmissionsPendingCount = submissionsPendingCount,
            TopCourses = topCourses,
            News = newsFeed,
            NewsForm = newsForm ?? new CreateNewsViewModel()
        };
    }

    private async Task<string> SaveNewsImageAsync(IFormFile file, string extension)
    {
        var uploadsDir = GetNewsStorageDirectory();
        Directory.CreateDirectory(uploadsDir);

        if (string.IsNullOrWhiteSpace(extension))
            throw new InvalidOperationException("Файл не распознан как изображение JPG, PNG или WEBP.");

        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsDir, fileName);
        var tempPath = Path.Combine(uploadsDir, $"{Guid.NewGuid():N}.tmp");

        try
        {
            await using var source = file.OpenReadStream();
            await using (var destination = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 64, useAsync: true))
            {
                await source.CopyToAsync(destination);
                await destination.FlushAsync();
            }

            System.IO.File.Move(tempPath, fullPath);
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
        }

        return $"/Media/News/{fileName}";
    }

    private async Task<string?> DetectNewsImageExtensionAsync(IFormFile file)
    {
        if (file.Length <= 0)
            return null;

        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
        if (read < 3)
            return null;

        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return ".jpg";

        if (read >= 8 &&
            header[0] == 0x89 &&
            header[1] == 0x50 &&
            header[2] == 0x4E &&
            header[3] == 0x47 &&
            header[4] == 0x0D &&
            header[5] == 0x0A &&
            header[6] == 0x1A &&
            header[7] == 0x0A)
            return ".png";

        if (read >= 12 &&
            header[0] == 0x52 &&
            header[1] == 0x49 &&
            header[2] == 0x46 &&
            header[3] == 0x46 &&
            header[8] == 0x57 &&
            header[9] == 0x45 &&
            header[10] == 0x42 &&
            header[11] == 0x50)
            return ".webp";

        return null;
    }

    private string GetNewsStorageDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
            localAppData = _environment.ContentRootPath;

        return Path.Combine(localAppData, "ISPO.WebApp", "uploads", "news");
    }

    private string GetNewsLogFilePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
            localAppData = _environment.ContentRootPath;

        var logDir = Path.Combine(localAppData, "ISPO.WebApp", "logs");
        Directory.CreateDirectory(logDir);
        return Path.Combine(logDir, "news-upload.log");
    }

    private void AppendNewsUploadLog(string message)
    {
        try
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {message}{Environment.NewLine}";
            System.IO.File.AppendAllText(GetNewsLogFilePath(), line);
        }
        catch
        {
            // Лог не должен ломать рабочий сценарий.
        }
    }

    private string? ResolveNewsImageFullPath(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return null;

        var normalized = imageUrl.Trim();
        if (normalized.StartsWith("/Media/News/", StringComparison.OrdinalIgnoreCase))
        {
            var fileName = normalized["/Media/News/".Length..];
            if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains('/') || fileName.Contains('\\'))
                return null;

            return Path.Combine(GetNewsStorageDirectory(), fileName);
        }

        if (normalized.StartsWith("/uploads/news/", StringComparison.OrdinalIgnoreCase))
        {
            var webRootPath = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
                webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");

            var relativePath = normalized.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(webRootPath, relativePath);
        }

        return null;
    }

    private async Task<UserManagementViewModel> BuildUsersViewModelForValidationAsync(CreateUserViewModel form)
    {
        var users = await _db.Users.AsNoTracking().OrderByDescending(u => u.CreatedAt).ToListAsync();
        var studentsByEmail = await _db.Students
            .AsNoTracking()
            .Where(s => s.Email != null && s.GroupId != 0)
            .Select(s => new { Email = s.Email!.Trim().ToLower(), s.GroupId })
            .ToListAsync();

        var userGroupIds = new Dictionary<int, int?>();
        foreach (var user in users)
        {
            var groupId = studentsByEmail
                .FirstOrDefault(s => string.Equals(s.Email, user.Email?.Trim().ToLower(), StringComparison.Ordinal))?.GroupId;
            userGroupIds[user.Id] = groupId;
        }

        return new UserManagementViewModel
        {
            UserFullName = HttpContext.Session.GetString(AppSession.UserName) ?? "Администратор",
            Users = users,
            Groups = await _db.StudentGroups.AsNoTracking().OrderBy(g => g.GroupCode).ToListAsync(),
            UserGroupIds = userGroupIds,
            Form = form
        };
    }

    private void ValidateScheduleForm(CreateScheduleViewModel form)
    {
        if (form.EndTime <= form.StartTime)
            ModelState.AddModelError("Form.EndTime", "Время окончания должно быть позже времени начала.");
    }

    private async Task<AdminCoursesViewModel> BuildCoursesViewModelAsync(CreateCourseViewModel? form = null)
    {
        return new AdminCoursesViewModel
        {
            Teachers = await _db.Teachers
                .AsNoTracking()
                .OrderBy(t => t.FullName)
                .ToListAsync(),
            Categories = await _db.CourseCategories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync(),
            Courses = await _db.Courses
                .AsNoTracking()
                .Include(c => c.Teacher)
                .Include(c => c.Category)
                .OrderBy(c => c.Title)
                .Select(c => new AdminCourseListItemVm
                {
                    Id = c.Id,
                    CourseCode = c.CourseCode,
                    Title = c.Title,
                    Description = c.Description,
                    DurationHours = c.DurationHours,
                    IsActive = c.IsActive,
                    CategoryId = c.CategoryId,
                    CategoryName = c.Category.Name,
                    TeacherId = c.TeacherId,
                    TeacherName = c.Teacher.FullName,
                    EnrollmentsCount = c.Enrollments.Count,
                    LessonsCount = c.Schedules.Count
                })
                .ToListAsync(),
            Form = form ?? new CreateCourseViewModel()
        };
    }

    private async Task<AdminScheduleViewModel> BuildScheduleViewModelAsync(CreateScheduleViewModel? form = null)
    {
        return new AdminScheduleViewModel
        {
            Groups = await _db.StudentGroups.AsNoTracking().OrderBy(g => g.GroupCode).ToListAsync(),
            Courses = await _db.Courses
                .AsNoTracking()
                .Include(c => c.Teacher)
                .OrderBy(c => c.Title)
                .ToListAsync(),
            Items = await _db.Schedules
                .AsNoTracking()
                .Include(s => s.Group)
                .Include(s => s.Course)
                .ThenInclude(c => c.Teacher)
                .OrderBy(s => s.LessonDate)
                .ThenBy(s => s.StartTime)
                .Select(s => new ScheduleListItemVm
                {
                    Id = s.Id,
                    CourseId = s.CourseId,
                    GroupId = s.GroupId,
                    CourseTitle = s.Course.Title,
                    TeacherName = s.Course.Teacher.FullName,
                    GroupCode = s.Group.GroupCode,
                    LessonDate = s.LessonDate,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Room = s.Room,
                    LessonTopic = s.LessonTopic,
                    ConferenceUrl = s.ConferenceUrl,
                    LiveStatus = s.LiveStatus
                })
                .ToListAsync(),
            Form = form ?? new CreateScheduleViewModel()
        };
    }

    private async Task<AdminEnrollmentsViewModel> BuildEnrollmentsViewModelAsync(CreateEnrollmentViewModel form, string? statusFilter)
    {
        var query = _db.Enrollments
            .AsNoTracking()
            .Include(e => e.Student)
            .ThenInclude(s => s.Group)
            .Include(e => e.Course)
            .ThenInclude(c => c.Teacher)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(statusFilter))
            query = query.Where(e => e.Status == statusFilter);

        var enrolledStudentsQuery = _db.Enrollments
            .AsNoTracking()
            .Select(e => e.StudentId);

        return new AdminEnrollmentsViewModel
        {
            StatusFilter = statusFilter,
            Students = await _db.Students
                .AsNoTracking()
                .Include(s => s.Group)
                .Where(s => !enrolledStudentsQuery.Contains(s.Id))
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync(),
            Courses = await _db.Courses
                .AsNoTracking()
                .OrderBy(c => c.Title)
                .ToListAsync(),
            Items = await query
                .OrderByDescending(e => e.EnrolledAt)
                .Select(e => new EnrollmentListItemVm
                {
                    Id = e.Id,
                    StudentId = e.StudentId,
                    CourseId = e.CourseId,
                    StudentName = (e.Student.LastName + " " + e.Student.FirstName + " " + (e.Student.MiddleName ?? string.Empty)).Trim(),
                    GroupCode = e.Student.Group.GroupCode,
                    CourseTitle = e.Course.Title,
                    TeacherName = e.Course.Teacher.FullName,
                    EnrolledAt = e.EnrolledAt,
                    Status = e.Status
                })
                .ToListAsync(),
            Form = form
        };
    }

    private IActionResult RenderReport(AdminReportViewModel vm, string? format, string fileName)
    {
        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csv = BuildCsv(vm.Headers, vm.Rows);
            return File(BuildCsvBytesWithBom(csv), "text/csv; charset=utf-8", fileName);
        }

        if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            var pdfBytes = BuildReportPdf(vm);
            var pdfFileName = Path.ChangeExtension(fileName, ".pdf");
            return File(pdfBytes, "application/pdf", pdfFileName);
        }

        return View("Reports", vm);
    }

    private static byte[] BuildCsvBytesWithBom(string csvContent)
    {
        var utf8Preamble = Encoding.UTF8.GetPreamble();
        var utf8Bytes = Encoding.UTF8.GetBytes(csvContent);
        var result = new byte[utf8Preamble.Length + utf8Bytes.Length];
        Buffer.BlockCopy(utf8Preamble, 0, result, 0, utf8Preamble.Length);
        Buffer.BlockCopy(utf8Bytes, 0, result, utf8Preamble.Length, utf8Bytes.Length);
        return result;
    }

    private static byte[] BuildReportPdf(AdminReportViewModel vm)
    {
        static IContainer HeaderCellStyle(IContainer container)
            => container
                .Background(Colors.Green.Lighten4)
                .Border(1)
                .BorderColor(Colors.Green.Lighten2)
                .PaddingVertical(6)
                .PaddingHorizontal(8);

        static IContainer DataCellStyle(IContainer container)
            => container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(5)
                .PaddingHorizontal(8);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(text => text.FontFamily("Arial").FontSize(10));

                page.Header().Column(column =>
                {
                    column.Spacing(2);
                    column.Item().Text(vm.ReportTitle).FontSize(16).SemiBold();
                    column.Item().Text($"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm}");
                    column.Item().Text($"Период: {FormatReportDate(vm.Filter.DateFrom)} - {FormatReportDate(vm.Filter.DateTo)}");
                });

                page.Content().PaddingTop(10).Table(table =>
                {
                    var totalColumns = Math.Max(1, vm.Headers.Count);
                    table.ColumnsDefinition(columns =>
                    {
                        for (var i = 0; i < totalColumns; i++)
                            columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (var headerText in vm.Headers)
                        {
                            header.Cell()
                                .Element(HeaderCellStyle)
                                .Text(headerText)
                                .SemiBold();
                        }
                    });

                    if (vm.Rows.Count == 0)
                    {
                        table.Cell()
                            .ColumnSpan((uint)totalColumns)
                            .Element(DataCellStyle)
                            .Text(vm.EmptyMessage);
                        return;
                    }

                    foreach (var row in vm.Rows)
                    {
                        for (var columnIndex = 0; columnIndex < totalColumns; columnIndex++)
                        {
                            var value = columnIndex < row.Count ? row[columnIndex] : string.Empty;
                            table.Cell()
                                .Element(DataCellStyle)
                                .Text(value);
                        }
                    }
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string FormatReportDate(DateTime? date)
        => date.HasValue ? date.Value.ToString("dd.MM.yyyy") : "не задано";

    private static string BuildCsv(IReadOnlyList<string> headers, IReadOnlyList<List<string>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', headers.Select(EscapeCsv)));
        foreach (var row in rows)
            sb.AppendLine(string.Join(',', row.Select(EscapeCsv)));

        return sb.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private async Task<(List<ReportSelectItemVm> groups, List<ReportSelectItemVm> courses)> BuildReportLookupsAsync()
    {
        var groups = await _db.StudentGroups
            .AsNoTracking()
            .OrderBy(g => g.GroupCode)
            .Select(g => new ReportSelectItemVm { Id = g.Id, Name = g.GroupCode + " - " + g.Name })
            .ToListAsync();

        var courses = await _db.Courses
            .AsNoTracking()
            .OrderBy(c => c.Title)
            .Select(c => new ReportSelectItemVm { Id = c.Id, Name = c.CourseCode + " - " + c.Title })
            .ToListAsync();

        return (groups, courses);
    }

    private async Task<List<ReportStudentVm>> BuildStudentsForReportsAsync(int? groupId, int? courseId)
    {
        var studentsQuery = _db.Students
            .AsNoTracking()
            .Include(s => s.Group)
            .AsQueryable();

        if (groupId.HasValue)
            studentsQuery = studentsQuery.Where(s => s.GroupId == groupId.Value);

        if (courseId.HasValue)
        {
            studentsQuery = studentsQuery.Where(s =>
                s.Enrollments.Any(e => e.CourseId == courseId.Value) ||
                _db.Schedules.Any(sc => sc.CourseId == courseId.Value && sc.GroupId == s.GroupId));
        }

        return await studentsQuery
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Select(s => new ReportStudentVm
            {
                StudentId = s.Id,
                StudentName = (s.LastName + " " + s.FirstName + " " + (s.MiddleName ?? string.Empty)).Trim(),
                GroupCode = s.Group.GroupCode
            })
            .ToListAsync();
    }

    private async Task<string> ResolveReportCourseTitleAsync(int? courseId)
    {
        if (!courseId.HasValue)
            return "Все курсы";

        return await _db.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId.Value)
            .Select(c => c.Title)
            .FirstOrDefaultAsync() ?? "Курс не найден";
    }

    private async Task<AdminReportViewModel> BuildPerformanceReportAsync(DateTime? dateFrom, DateTime? dateTo, int? groupId, int? courseId)
    {
        var lookups = await BuildReportLookupsAsync();
        var students = await BuildStudentsForReportsAsync(groupId, courseId);
        var selectedCourseTitle = await ResolveReportCourseTitleAsync(courseId);

        var submissionsQuery = _db.LessonSubmissions
            .AsNoTracking()
            .Where(s => s.Score != null && s.Status == AppConstants.SubmissionStatus.Accepted)
            .AsQueryable();

        if (dateFrom.HasValue)
            submissionsQuery = submissionsQuery.Where(s => (s.ReviewedAt ?? s.SubmittedAt) >= dateFrom.Value.Date);
        if (dateTo.HasValue)
        {
            var endExclusive = dateTo.Value.Date.AddDays(1);
            submissionsQuery = submissionsQuery.Where(s => (s.ReviewedAt ?? s.SubmittedAt) < endExclusive);
        }

        if (groupId.HasValue)
            submissionsQuery = submissionsQuery.Where(s => s.Student.GroupId == groupId.Value);
        if (courseId.HasValue)
            submissionsQuery = submissionsQuery.Where(s => s.Schedule.CourseId == courseId.Value);

        var submissionsByStudent = await submissionsQuery
            .GroupBy(s => s.StudentId)
            .Select(g => new
            {
                StudentId = g.Key,
                AvgGrade = g.Average(x => x.Score ?? 0m),
                GradesCount = g.Count()
            })
            .ToDictionaryAsync(x => x.StudentId, x => new ReportStudentMetric
            {
                AvgValue = x.AvgGrade,
                Count = x.GradesCount
            });

        var rows = students
            .Select(s =>
            {
                var hasSubmissions = submissionsByStudent.TryGetValue(s.StudentId, out var submissionsMetric);

                var metric = hasSubmissions
                    ? submissionsMetric!
                    : new ReportStudentMetric { AvgValue = 0m, Count = 0 };

                return new List<string>
                {
                    s.StudentName,
                    s.GroupCode,
                    selectedCourseTitle,
                    metric.AvgValue.ToString("0.0"),
                    metric.Count.ToString()
                };
            })
            .ToList();

        return new AdminReportViewModel
        {
            ReportKey = "performance",
            ReportTitle = "Отчет по успеваемости",
            Filter = new ReportFilterVm { DateFrom = dateFrom, DateTo = dateTo, GroupId = groupId, CourseId = courseId },
            Groups = lookups.groups,
            Courses = lookups.courses,
            Headers = ["Студент", "Группа", "Курс", "Средний балл", "Кол-во оценок"],
            Rows = rows
        };
    }
    private async Task<AdminReportViewModel> BuildAttendanceReportAsync(DateTime? dateFrom, DateTime? dateTo, int? groupId, int? courseId)
    {
        var lookups = await BuildReportLookupsAsync();
        var students = await BuildStudentsForReportsAsync(groupId, courseId);
        var selectedCourseTitle = await ResolveReportCourseTitleAsync(courseId);

        var attendanceQuery = _db.Attendances
            .AsNoTracking()
            .AsQueryable();

        if (dateFrom.HasValue)
            attendanceQuery = attendanceQuery.Where(a => a.Schedule.LessonDate >= dateFrom.Value.Date);
        if (dateTo.HasValue)
            attendanceQuery = attendanceQuery.Where(a => a.Schedule.LessonDate <= dateTo.Value.Date);
        if (groupId.HasValue)
            attendanceQuery = attendanceQuery.Where(a => a.Student.GroupId == groupId.Value);
        if (courseId.HasValue)
            attendanceQuery = attendanceQuery.Where(a => a.Schedule.CourseId == courseId.Value);

        var attendanceByStudent = await attendanceQuery
            .GroupBy(a => a.StudentId)
            .Select(g => new
            {
                StudentId = g.Key,
                Total = g.Count(),
                Present = g.Count(x => x.IsPresent)
            })
            .ToDictionaryAsync(x => x.StudentId, x => new
            {
                x.Total,
                x.Present
            });

        var rows = students
            .Select(s =>
            {
                var stat = attendanceByStudent.TryGetValue(s.StudentId, out var value)
                    ? value
                    : new { Total = 0, Present = 0 };

                var percent = stat.Total == 0 ? 0m : stat.Present * 100m / stat.Total;
                return new List<string>
                {
                    s.StudentName,
                    s.GroupCode,
                    selectedCourseTitle,
                    stat.Present.ToString(),
                    stat.Total.ToString(),
                    percent.ToString("0.0") + "%"
                };
            })
            .ToList();

        return new AdminReportViewModel
        {
            ReportKey = "attendance",
            ReportTitle = "Отчет по посещаемости",
            Filter = new ReportFilterVm { DateFrom = dateFrom, DateTo = dateTo, GroupId = groupId, CourseId = courseId },
            Groups = lookups.groups,
            Courses = lookups.courses,
            Headers = ["Студент", "Группа", "Курс", "Присутствий", "Всего", "Посещаемость"],
            Rows = rows
        };
    }

    private async Task<AdminReportViewModel> BuildCourseActivityReportAsync(DateTime? dateFrom, DateTime? dateTo, int? groupId, int? courseId)
    {
        var lookups = await BuildReportLookupsAsync();

        var coursesQuery = _db.Courses
            .AsNoTracking()
            .Include(c => c.Teacher)
            .Include(c => c.Enrollments)
            .Include(c => c.Schedules)
            .ThenInclude(s => s.LessonSubmissions)
            .AsQueryable();

        if (courseId.HasValue)
            coursesQuery = coursesQuery.Where(c => c.Id == courseId.Value);
        if (groupId.HasValue)
            coursesQuery = coursesQuery.Where(c => c.Schedules.Any(s => s.GroupId == groupId.Value));
        if (dateFrom.HasValue)
            coursesQuery = coursesQuery.Where(c => c.Schedules.Any(s => s.LessonDate >= dateFrom.Value.Date));
        if (dateTo.HasValue)
            coursesQuery = coursesQuery.Where(c => c.Schedules.Any(s => s.LessonDate <= dateTo.Value.Date));

        var rows = await coursesQuery
            .Select(c => new
            {
                c.CourseCode,
                c.Title,
                Teacher = c.Teacher.FullName,
                Enrolled = c.Enrollments.Count,
                Lessons = c.Schedules.Count,
                LiveLessons = c.Schedules.Count(s => s.LiveStatus == AppConstants.LessonLiveStatus.Live),
                Submissions = c.Schedules.SelectMany(s => s.LessonSubmissions).Count(),
                Accepted = c.Schedules.SelectMany(s => s.LessonSubmissions).Count(ls => ls.Status == AppConstants.SubmissionStatus.Accepted)
            })
            .OrderByDescending(r => r.Enrolled)
            .ThenBy(r => r.Title)
            .ToListAsync();

        return new AdminReportViewModel
        {
            ReportKey = "course-activity",
            ReportTitle = "Отчет по активности курсов",
            Filter = new ReportFilterVm { DateFrom = dateFrom, DateTo = dateTo, GroupId = groupId, CourseId = courseId },
            Groups = lookups.groups,
            Courses = lookups.courses,
            Headers = ["Код", "Курс", "Преподаватель", "Зачислено", "Занятий", "Онлайн", "Работ", "Принято"],
            Rows = rows.Select(r => new List<string>
            {
                r.CourseCode,
                r.Title,
                r.Teacher,
                r.Enrolled.ToString(),
                r.Lessons.ToString(),
                r.LiveLessons.ToString(),
                r.Submissions.ToString(),
                r.Accepted.ToString()
            }).ToList()
        };
    }

    private async Task<AdminReportViewModel> BuildTeacherLoadReportAsync(DateTime? dateFrom, DateTime? dateTo, int? groupId, int? courseId)
    {
        var lookups = await BuildReportLookupsAsync();

        var teacherQuery = _db.Teachers
            .AsNoTracking()
            .Include(t => t.Courses)
            .ThenInclude(c => c.Enrollments)
            .Include(t => t.Courses)
            .ThenInclude(c => c.Schedules)
            .AsQueryable();

        if (courseId.HasValue)
            teacherQuery = teacherQuery.Where(t => t.Courses.Any(c => c.Id == courseId.Value));
        if (groupId.HasValue)
            teacherQuery = teacherQuery.Where(t => t.Courses.Any(c => c.Schedules.Any(s => s.GroupId == groupId.Value)));
        if (dateFrom.HasValue)
            teacherQuery = teacherQuery.Where(t => t.Courses.Any(c => c.Schedules.Any(s => s.LessonDate >= dateFrom.Value.Date)));
        if (dateTo.HasValue)
            teacherQuery = teacherQuery.Where(t => t.Courses.Any(c => c.Schedules.Any(s => s.LessonDate <= dateTo.Value.Date)));

        var rows = await teacherQuery
            .Select(t => new
            {
                t.FullName,
                t.Department,
                CoursesCount = t.Courses.Count,
                LessonsCount = t.Courses.SelectMany(c => c.Schedules).Count(),
                StudentsCount = t.Courses.SelectMany(c => c.Enrollments).Select(e => e.StudentId).Distinct().Count()
            })
            .OrderByDescending(r => r.LessonsCount)
            .ThenBy(r => r.FullName)
            .ToListAsync();

        return new AdminReportViewModel
        {
            ReportKey = "teacher-load",
            ReportTitle = "Отчет по нагрузке преподавателей",
            Filter = new ReportFilterVm { DateFrom = dateFrom, DateTo = dateTo, GroupId = groupId, CourseId = courseId },
            Groups = lookups.groups,
            Courses = lookups.courses,
            Headers = ["Преподаватель", "Отделение", "Курсов", "Занятий", "Уникальных студентов"],
            Rows = rows.Select(r => new List<string>
            {
                r.FullName,
                r.Department,
                r.CoursesCount.ToString(),
                r.LessonsCount.ToString(),
                r.StudentsCount.ToString()
            }).ToList()
        };
    }

    private async Task<AdminReportViewModel> BuildGroupProgressReportAsync(DateTime? dateFrom, DateTime? dateTo, int? groupId, int? courseId)
    {
        var lookups = await BuildReportLookupsAsync();

        var groupsQuery = _db.StudentGroups
            .AsNoTracking()
            .Include(g => g.Students)
            .ThenInclude(s => s.Enrollments)
            .Include(g => g.Students)
            .ThenInclude(s => s.Attendances)
            .AsQueryable();

        if (groupId.HasValue)
            groupsQuery = groupsQuery.Where(g => g.Id == groupId.Value);
        if (courseId.HasValue)
            groupsQuery = groupsQuery.Where(g => g.Students.Any(s => s.Enrollments.Any(e => e.CourseId == courseId.Value)));

        var data = await groupsQuery
            .Select(g => new
            {
                g.GroupCode,
                g.Name,
                Students = g.Students.Count,
                ActiveEnrollments = g.Students.SelectMany(s => s.Enrollments).Count(e => e.Status == "active"),
                AvgGrade = g.Students
                    .SelectMany(s => s.LessonSubmissions)
                    .Where(ls =>
                        ls.Score != null &&
                        ls.Status == AppConstants.SubmissionStatus.Accepted &&
                        (!courseId.HasValue || ls.Schedule.CourseId == courseId.Value) &&
                        (!dateFrom.HasValue || (ls.ReviewedAt ?? ls.SubmittedAt) >= dateFrom.Value.Date) &&
                        (!dateTo.HasValue || (ls.ReviewedAt ?? ls.SubmittedAt) < dateTo.Value.Date.AddDays(1)))
                    .Average(ls => (decimal?)ls.Score),
                AttendanceTotal = g.Students.SelectMany(s => s.Attendances).Count(),
                AttendancePresent = g.Students.SelectMany(s => s.Attendances).Count(a => a.IsPresent),
                Submissions = g.Students
                    .SelectMany(s => s.LessonSubmissions)
                    .Count(ls =>
                        (!courseId.HasValue || ls.Schedule.CourseId == courseId.Value) &&
                        (!dateFrom.HasValue || (ls.ReviewedAt ?? ls.SubmittedAt) >= dateFrom.Value.Date) &&
                        (!dateTo.HasValue || (ls.ReviewedAt ?? ls.SubmittedAt) < dateTo.Value.Date.AddDays(1))),
                Accepted = g.Students
                    .SelectMany(s => s.LessonSubmissions)
                    .Count(ls =>
                        ls.Status == AppConstants.SubmissionStatus.Accepted &&
                        (!courseId.HasValue || ls.Schedule.CourseId == courseId.Value) &&
                        (!dateFrom.HasValue || (ls.ReviewedAt ?? ls.SubmittedAt) >= dateFrom.Value.Date) &&
                        (!dateTo.HasValue || (ls.ReviewedAt ?? ls.SubmittedAt) < dateTo.Value.Date.AddDays(1)))
            })
            .OrderBy(x => x.GroupCode)
            .ToListAsync();

        var rows = data.Select(d =>
        {
            var attendanceRate = d.AttendanceTotal == 0 ? 0m : d.AttendancePresent * 100m / d.AttendanceTotal;
            return new List<string>
            {
                d.GroupCode,
                d.Name,
                d.Students.ToString(),
                d.ActiveEnrollments.ToString(),
                (d.AvgGrade ?? 0m).ToString("0.0"),
                attendanceRate.ToString("0.0") + "%",
                d.Submissions.ToString(),
                d.Accepted.ToString()
            };
        }).ToList();

        return new AdminReportViewModel
        {
            ReportKey = "group-progress",
            ReportTitle = "Отчет по прогрессу групп",
            Filter = new ReportFilterVm { DateFrom = dateFrom, DateTo = dateTo, GroupId = groupId, CourseId = courseId },
            Groups = lookups.groups,
            Courses = lookups.courses,
            Headers = ["Группа", "Название", "Студентов", "Активных зачислений", "Ср. балл", "Посещаемость", "Работ", "Принято"],
            Rows = rows
        };
    }

    private sealed class ReportStudentVm
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string GroupCode { get; set; } = string.Empty;
    }

    private sealed class ReportStudentMetric
    {
        public decimal AvgValue { get; set; }
        public int Count { get; set; }
    }
}










