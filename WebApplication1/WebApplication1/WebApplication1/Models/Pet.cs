using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class Pet
{
    public int PetId { get; set; }

    [Required, StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string Species { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Breed { get; set; }

    [StringLength(20)]
    public string? Sex { get; set; }

    [DataType(DataType.Date)]
    public DateTime? BirthDate { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int OwnerId { get; set; }

    public Owner? Owner { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
