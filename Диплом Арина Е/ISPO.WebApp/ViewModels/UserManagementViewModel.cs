using System.ComponentModel.DataAnnotations;
using ISPO.WebApp.Models;

namespace ISPO.WebApp.ViewModels;

public class UserManagementViewModel
{
    public string UserFullName { get; set; } = string.Empty;
    public string? RoleFilter { get; set; }
    public List<UserAccount> Users { get; set; } = new();
    public List<StudentGroup> Groups { get; set; } = new();
    public Dictionary<int, int?> UserGroupIds { get; set; } = new();
    public CreateUserViewModel Form { get; set; } = new();
}

public class CreateUserViewModel
{
    public int? UserId { get; set; }

    [Required(ErrorMessage = "Укажите ФИО пользователя.")]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите адрес электронной почты.")]
    [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты.")]
    [StringLength(120)]
    public string Email { get; set; } = string.Empty;

    [StringLength(100, MinimumLength = 6, ErrorMessage = "Минимальная длина пароля - 6 символов.")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Выберите роль.")]
    [RegularExpression("admin|teacher|student", ErrorMessage = "Роль выбрана неверно.")]
    public string Role { get; set; } = "student";

    public int? GroupId { get; set; }
}
