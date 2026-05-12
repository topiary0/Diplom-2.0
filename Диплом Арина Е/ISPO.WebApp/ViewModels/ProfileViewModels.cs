using System.ComponentModel.DataAnnotations;

namespace ISPO.WebApp.ViewModels;

public class AdminSettingsViewModel
{
    public string CurrentName { get; set; } = string.Empty;
    public string CurrentEmail { get; set; } = string.Empty;
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int LiveLessons { get; set; }
    public int PendingSubmissions { get; set; }
    public int MissingTeacherAccounts { get; set; }
    public int MissingStudentAccounts { get; set; }
    public UpdateAdminSettingsForm Form { get; set; } = new();
    public AdminSiteSettingsForm SiteSettingsForm { get; set; } = new();
    public AdminPasswordResetForm PasswordResetForm { get; set; } = new();
    public AdminRoleAccessForm RoleAccessForm { get; set; } = new();
    public AdminSyncAccountsForm SyncAccountsForm { get; set; } = new();
}

public class AdminSiteSettingsForm
{
    [Required(ErrorMessage = "Выберите тему оформления.")]
    [RegularExpression("light|dark", ErrorMessage = "Недопустимое значение темы оформления.")]
    public string Theme { get; set; } = "light";

    [Required(ErrorMessage = "Выберите плотность интерфейса.")]
    [RegularExpression("normal|compact", ErrorMessage = "Недопустимое значение плотности интерфейса.")]
    public string InterfaceMode { get; set; } = "normal";
}

public class UpdateAdminSettingsForm
{
    [Required(ErrorMessage = "Укажите имя.")]
    [StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите адрес электронной почты.")]
    [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты.")]
    [StringLength(120)]
    public string Email { get; set; } = string.Empty;

    [StringLength(120, MinimumLength = 6, ErrorMessage = "Новый пароль должен быть длиной не менее 6 символов.")]
    public string? NewPassword { get; set; }

    [Compare(nameof(NewPassword), ErrorMessage = "Подтверждение пароля не совпадает.")]
    public string? ConfirmPassword { get; set; }
}

public class AdminPasswordResetForm
{
    [Required(ErrorMessage = "Укажите адрес электронной почты пользователя.")]
    [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты.")]
    [StringLength(120)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите новый пароль.")]
    [StringLength(120, MinimumLength = 6, ErrorMessage = "Пароль должен быть длиной не менее 6 символов.")]
    public string NewPassword { get; set; } = string.Empty;
}

public class AdminRoleAccessForm
{
    [Required(ErrorMessage = "Выберите роль.")]
    [RegularExpression("admin|teacher|student", ErrorMessage = "Роль выбрана неверно.")]
    public string Role { get; set; } = "teacher";

    public bool EnableAccess { get; set; } = true;
}

public class AdminSyncAccountsForm
{
    public bool IncludeTeachers { get; set; } = true;
    public bool IncludeStudents { get; set; } = true;

    [Required(ErrorMessage = "Укажите пароль по умолчанию.")]
    [StringLength(120, MinimumLength = 6, ErrorMessage = "Пароль по умолчанию должен быть длиной не менее 6 символов.")]
    public string DefaultPassword { get; set; } = "student123";
}

public class TeacherProfileViewModel
{
    public bool ProfileFound { get; set; }
    public int LessonsThisWeek { get; set; }
    public int PendingSubmissions { get; set; }
    public UpdateTeacherProfileForm Form { get; set; } = new();
}

public class TeacherSiteSettingsViewModel
{
    public TeacherSiteSettingsForm Form { get; set; } = new();
}

public class UpdateTeacherProfileForm
{
    [Required(ErrorMessage = "Укажите ФИО.")]
    [StringLength(180)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите адрес электронной почты.")]
    [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты.")]
    [StringLength(120)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите отделение.")]
    [StringLength(150)]
    public string Department { get; set; } = string.Empty;

    [StringLength(120)]
    public string? PositionName { get; set; }

    [StringLength(120, MinimumLength = 6, ErrorMessage = "Новый пароль должен быть длиной не менее 6 символов.")]
    public string? NewPassword { get; set; }

    [Compare(nameof(NewPassword), ErrorMessage = "Подтверждение пароля не совпадает.")]
    public string? ConfirmPassword { get; set; }

}

public class TeacherSiteSettingsForm
{
    [Required(ErrorMessage = "Выберите тему оформления.")]
    [RegularExpression("light|dark", ErrorMessage = "Недопустимое значение темы оформления.")]
    public string Theme { get; set; } = "light";

    [Required(ErrorMessage = "Выберите плотность интерфейса.")]
    [RegularExpression("normal|compact", ErrorMessage = "Недопустимое значение плотности интерфейса.")]
    public string InterfaceMode { get; set; } = "normal";
}

public class StudentProfileViewModel
{
    public bool ProfileFound { get; set; }
    public int ActiveCourses { get; set; }
    public int PendingSubmissions { get; set; }
    public UpdateStudentProfileForm Form { get; set; } = new();
}

public class StudentSiteSettingsViewModel
{
    public StudentSiteSettingsForm Form { get; set; } = new();
}

public class UpdateStudentProfileForm
{
    [Required(ErrorMessage = "Укажите фамилию.")]
    [StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите имя.")]
    [StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(80)]
    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "Укажите дату рождения.")]
    public DateTime BirthDate { get; set; } = DateTime.Today.AddYears(-18);

    [Required(ErrorMessage = "Укажите адрес электронной почты.")]
    [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты.")]
    [StringLength(120)]
    public string Email { get; set; } = string.Empty;

    [StringLength(40)]
    public string? Phone { get; set; }

    [StringLength(120, MinimumLength = 6, ErrorMessage = "Новый пароль должен быть длиной не менее 6 символов.")]
    public string? NewPassword { get; set; }

    [Compare(nameof(NewPassword), ErrorMessage = "Подтверждение пароля не совпадает.")]
    public string? ConfirmPassword { get; set; }

}

public class StudentSiteSettingsForm
{
    [Required(ErrorMessage = "Выберите тему оформления.")]
    [RegularExpression("light|dark", ErrorMessage = "Недопустимое значение темы оформления.")]
    public string Theme { get; set; } = "light";

    [Required(ErrorMessage = "Выберите плотность интерфейса.")]
    [RegularExpression("normal|compact", ErrorMessage = "Недопустимое значение плотности интерфейса.")]
    public string InterfaceMode { get; set; } = "normal";
}
