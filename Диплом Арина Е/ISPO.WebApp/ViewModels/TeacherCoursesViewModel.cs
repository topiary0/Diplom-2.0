using System.ComponentModel.DataAnnotations;
using ISPO.WebApp.Models;

namespace ISPO.WebApp.ViewModels;

public class TeacherCoursesViewModel
{
    public string UserFullName { get; set; } = string.Empty;
    public List<Teacher> Teachers { get; set; } = new();
    public List<CourseCategory> Categories { get; set; } = new();
    public List<CourseListItemVm> Courses { get; set; } = new();
    public CreateCourseViewModel Form { get; set; } = new();
}

public class CourseListItemVm
{
    public int Id { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int DurationHours { get; set; }
    public bool IsActive { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
}

public class CreateCourseViewModel
{
    [Required(ErrorMessage = "Укажите код курса.")]
    [StringLength(40)]
    public string CourseCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите название курса.")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(1, 1000, ErrorMessage = "Количество часов должно быть от 1 до 1000.")]
    public int DurationHours { get; set; } = 36;

    [Range(1, int.MaxValue, ErrorMessage = "Выберите категорию.")]
    public int CategoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Выберите преподавателя.")]
    public int TeacherId { get; set; }
}
