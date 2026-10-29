using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

// Записва метаданните за файл към прегледа; самият файл стои извън публичната папка.
public class ClinicFile
{
    public int ClinicFileId { get; set; }

    [Required, StringLength(180)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string StoredFileName { get; set; } = string.Empty;

    [StringLength(80)]
    public string? ThumbnailFileName { get; set; }

    [Required, StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    [Range(1, 10_485_760)]
    public long SizeBytes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Range(1, int.MaxValue)]
    public int AppointmentId { get; set; }

    [Required]
    public string UploadedByUserId { get; set; } = string.Empty;

    public Appointment? Appointment { get; set; }
    public ApplicationUser? UploadedByUser { get; set; }
}
