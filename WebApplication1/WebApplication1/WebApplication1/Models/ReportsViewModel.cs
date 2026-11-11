namespace WebApplication1.Models;

// Пази обобщените данни за годишната справка.
public class ReportsViewModel
{
    public int Year { get; set; }
    public IReadOnlyList<int> AvailableYears { get; set; } = Array.Empty<int>();
    public int AppointmentCount { get; set; }
    public int CompletedAppointmentCount { get; set; }
    public int TreatmentCount { get; set; }
    public decimal TotalTreatmentValue { get; set; }
    public IReadOnlyList<MonthlyReportItem> Months { get; set; } = Array.Empty<MonthlyReportItem>();
    public IReadOnlyList<ServiceReportItem> TopServices { get; set; } = Array.Empty<ServiceReportItem>();
}

// Данните за една колона в месечната графика.
public class MonthlyReportItem
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int AppointmentCount { get; set; }
    public int CompletedAppointmentCount { get; set; }
    public int TreatmentCount { get; set; }
    public decimal TreatmentValue { get; set; }
    public int ValueBarWidth { get; set; }
}

// Брой и стойност на най-търсените процедури.
public class ServiceReportItem
{
    public string Name { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public decimal TotalValue { get; set; }
    public int BarWidth { get; set; }
}
