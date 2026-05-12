using System.ComponentModel.DataAnnotations;

namespace ISPO.WebApp.ViewModels;

public class SupportCenterViewModel
{
    public string RoleTitle { get; set; } = string.Empty;
    public CreateSupportRequestForm Form { get; set; } = new();
    public List<SupportRequestItemVm> Items { get; set; } = new();
}

public class AdminSupportRequestsViewModel
{
    public string? StatusFilter { get; set; }
    public int TotalCount { get; set; }
    public int NewCount { get; set; }
    public int InProgressCount { get; set; }
    public int ClosedCount { get; set; }
    public List<SupportRequestItemVm> Items { get; set; } = new();
}

public class SupportRequestItemVm
{
    public int Id { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AdminComment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateSupportRequestForm
{
    [Required(ErrorMessage = "Укажите тему обращения.")]
    [StringLength(180, MinimumLength = 4, ErrorMessage = "Тема обращения должна быть от 4 до 180 символов.")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите текст обращения.")]
    [StringLength(4000, MinimumLength = 10, ErrorMessage = "Текст обращения должен быть от 10 до 4000 символов.")]
    public string Message { get; set; } = string.Empty;
}

