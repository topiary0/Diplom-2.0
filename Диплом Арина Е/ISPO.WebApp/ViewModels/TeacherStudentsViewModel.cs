using System.ComponentModel.DataAnnotations;
using ISPO.WebApp.Models;

namespace ISPO.WebApp.ViewModels;

public class TeacherStudentsViewModel
{
    public string UserFullName { get; set; } = string.Empty;
    public List<StudentGroup> Groups { get; set; } = new();
    public List<StudentListItemVm> Students { get; set; } = new();
    public CreateStudentViewModel Form { get; set; } = new();
}

public class StudentListItemVm
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string GroupCode { get; set; } = string.Empty;
}

public class CreateStudentViewModel
{
    [Required(ErrorMessage = "Введите фамилию.")]
    [StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите имя.")]
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

    [StringLength(30)]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Укажите пароль для входа.")]
    [StringLength(120, MinimumLength = 6, ErrorMessage = "Минимальная длина пароля — 6 символов.")]
    public string InitialPassword { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Выберите группу.")]
    public int GroupId { get; set; }
}

