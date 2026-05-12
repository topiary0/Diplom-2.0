using System.ComponentModel.DataAnnotations;

namespace ISPO.WebApp.ViewModels;

public class NewsCardVm
{
    public int Id { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateNewsViewModel
{
    [Required(ErrorMessage = "Введите текст новости.")]
    [StringLength(4000, ErrorMessage = "Текст новости не должен превышать 4000 символов.")]
    public string Body { get; set; } = string.Empty;

}

public class NewsPageViewModel
{
    public string UserFullName { get; set; } = string.Empty;
    public bool CanManageNews { get; set; }
    public List<NewsCardVm> News { get; set; } = new();
    public CreateNewsViewModel NewsForm { get; set; } = new();
}
