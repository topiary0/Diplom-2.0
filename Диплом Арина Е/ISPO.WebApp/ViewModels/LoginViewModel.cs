using System.ComponentModel.DataAnnotations;

namespace ISPO.WebApp.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Введите адрес электронной почты.")]
    [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}


