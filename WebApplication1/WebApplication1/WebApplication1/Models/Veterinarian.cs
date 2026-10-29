using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

// Лекарят може да има много прегледи.
public class Veterinarian
{
    public int VeterinarianId { get; set; }

    [Required, StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Specialty { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
