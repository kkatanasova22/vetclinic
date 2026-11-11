using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[Authorize(Roles = "Admin")]
public class ReportsController : Controller
{
    private readonly VetClinicDbContext _context;

    public ReportsController(VetClinicDbContext context) => _context = context;

    // Събира годишните показатели чрез LINQ към базата.
    public async Task<IActionResult> Index(int? year)
    {
        return View(await BuildReportAsync(year));
    }

    // Сваля справката като PDF.
    public async Task<IActionResult> ExportPdf(int? year)
    {
        var report = await BuildReportAsync(year);
        var file = ReportExportService.CreatePdf(report);
        return File(file, "application/pdf", $"vetclinic-report-{report.Year}.pdf");
    }

    // Сваля справката като Excel файл.
    public async Task<IActionResult> ExportExcel(int? year)
    {
        var report = await BuildReportAsync(year);
        var file = ReportExportService.CreateExcel(report);
        return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"vetclinic-report-{report.Year}.xlsx");
    }

    private async Task<ReportsViewModel> BuildReportAsync(int? requestedYear)
    {
        var currentYear = DateTime.Today.Year;
        var availableYears = await _context.Appointments.AsNoTracking()
            .Select(appointment => appointment.AppointmentDate.Year)
            .Where(value => value <= currentYear)
            .Distinct()
            .ToListAsync();
        availableYears.Add(currentYear);
        var years = availableYears.Distinct().OrderByDescending(value => value).ToArray();
        var selectedYear = requestedYear.HasValue && years.Contains(requestedYear.Value)
            ? requestedYear.Value
            : currentYear;
        var start = new DateTime(selectedYear, 1, 1);
        var end = start.AddYears(1);

        var appointments = _context.Appointments.AsNoTracking()
            .Where(appointment => appointment.AppointmentDate >= start && appointment.AppointmentDate < end);
        var monthlyAppointments = await appointments
            .GroupBy(appointment => appointment.AppointmentDate.Month)
            .Select(group => new
            {
                Month = group.Key,
                Count = group.Count(),
                CompletedCount = group.Count(appointment => appointment.Status == "Completed")
            })
            .ToListAsync();

        // Процедурите са източникът на стойността; отделни плащания не се пазят.
        var treatments = _context.Treatments.AsNoTracking()
            .Where(treatment => treatment.Appointment!.AppointmentDate >= start
                && treatment.Appointment.AppointmentDate < end
                && treatment.Appointment.Status != "Cancelled");
        var monthlyTreatments = await treatments
            .GroupBy(treatment => treatment.Appointment!.AppointmentDate.Month)
            .Select(group => new
            {
                Month = group.Key,
                Count = group.Count(),
                Value = group.Sum(treatment => treatment.Price)
            })
            .ToListAsync();
        var serviceRows = await treatments
            .GroupBy(treatment => treatment.TreatmentName)
            .Select(group => new
            {
                Name = group.Key,
                Count = group.Count(),
                Value = group.Sum(treatment => treatment.Price)
            })
            .OrderByDescending(service => service.Count)
            .ThenByDescending(service => service.Value)
            .ThenBy(service => service.Name)
            .Take(5)
            .ToListAsync();

        var monthlyByMonth = monthlyAppointments.ToDictionary(row => row.Month);
        var treatmentByMonth = monthlyTreatments.ToDictionary(row => row.Month);
        var maxValue = monthlyTreatments.Count == 0 ? 0m : monthlyTreatments.Max(row => row.Value);
        var monthRows = Enumerable.Range(1, 12).Select(month =>
        {
            monthlyByMonth.TryGetValue(month, out var appointmentRow);
            treatmentByMonth.TryGetValue(month, out var treatmentRow);
            var value = treatmentRow?.Value ?? 0m;
            return new MonthlyReportItem
            {
                Month = month,
                MonthName = CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(month),
                AppointmentCount = appointmentRow?.Count ?? 0,
                CompletedAppointmentCount = appointmentRow?.CompletedCount ?? 0,
                TreatmentCount = treatmentRow?.Count ?? 0,
                TreatmentValue = value,
                ValueBarWidth = maxValue <= 0 ? 0 : Math.Max(3, (int)Math.Round(value / maxValue * 100))
            };
        }).ToArray();
        var maxServiceCount = serviceRows.Count == 0 ? 0 : serviceRows.Max(row => row.Count);

        return new ReportsViewModel
        {
            Year = selectedYear,
            AvailableYears = years,
            AppointmentCount = monthRows.Sum(row => row.AppointmentCount),
            CompletedAppointmentCount = monthRows.Sum(row => row.CompletedAppointmentCount),
            TreatmentCount = monthlyTreatments.Sum(row => row.Count),
            TotalTreatmentValue = monthlyTreatments.Sum(row => row.Value),
            Months = monthRows,
            TopServices = serviceRows.Select(row => new ServiceReportItem
            {
                Name = row.Name,
                RequestCount = row.Count,
                TotalValue = row.Value,
                BarWidth = maxServiceCount == 0 ? 0 : Math.Max(3, (int)Math.Round(row.Count / (double)maxServiceCount * 100))
            }).ToArray()
        };
    }
}
