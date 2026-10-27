using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class Treatment
{
    public int TreatmentId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int AppointmentId { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string TreatmentName { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "99999999.99")]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    public Appointment? Appointment { get; set; }
}
