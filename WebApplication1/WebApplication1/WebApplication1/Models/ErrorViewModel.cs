namespace WebApplication1.Models;

// Показва кратък номер за търсене на грешка в журнала.
public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
