using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

// Данните за нов клиент; паролата не се записва в тази форма като текст в базата.
public class RegisterViewModel
{
    [Required, StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [Required, Phone, StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}
