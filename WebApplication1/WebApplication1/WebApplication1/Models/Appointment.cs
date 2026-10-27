using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class Appointment
{
    public int AppointmentId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int PetId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int VeterinarianId { get; set; }

    [Required, DataType(DataType.DateTime)]
    public DateTime AppointmentDate { get; set; }

    [StringLength(255)]
    public string? Diagnosis { get; set; }

    [Required, StringLength(20)]
    public string Status { get; set; } = "Confirmed";

    public Pet? Pet { get; set; }
    public Veterinarian? Veterinarian { get; set; }
    public ICollection<Treatment> Treatments { get; set; } = new List<Treatment>();
}
