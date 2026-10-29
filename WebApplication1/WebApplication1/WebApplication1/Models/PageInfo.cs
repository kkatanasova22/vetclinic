namespace WebApplication1.Models;

// Пази номерата за страниците, за да не зареждаме целия списък наведнъж.
public class PageInfo
{
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public string? Search { get; set; }
    public string? Sort { get; set; }
    public string? Status { get; set; }
    public string? Category { get; set; }
}
