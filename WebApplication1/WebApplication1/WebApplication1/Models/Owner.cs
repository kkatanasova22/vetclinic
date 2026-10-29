using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class Owner
{
    // Собственикът може да има профил за вход в системата.
    public int OwnerId { get; set; }

    [Required, StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    public ICollection<Pet> Pets { get; set; } = new List<Pet>();

    public ClientProfile? ClientProfile { get; set; }
}
