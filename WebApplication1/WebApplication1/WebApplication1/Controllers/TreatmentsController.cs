using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[Authorize(Roles = "Admin,Veterinarian")]
public class TreatmentsController : Controller
{
    private readonly VetClinicDbContext _context;

    public TreatmentsController(VetClinicDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? search)
    {
        var treatments = _context.Treatments.AsNoTracking()
            .Include(treatment => treatment.Appointment).ThenInclude(appointment => appointment!.Pet)
            .Include(treatment => treatment.Appointment).ThenInclude(appointment => appointment!.Veterinarian)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            treatments = treatments.Where(treatment => treatment.TreatmentName.Contains(term)
                || treatment.Appointment!.Pet!.Name.Contains(term)
                || treatment.Appointment.Veterinarian!.LastName.Contains(term));
        }

        ViewData["Search"] = search;
        ViewData["TreatmentCount"] = await _context.Treatments.CountAsync();
        ViewData["TotalValue"] = await _context.Treatments.SumAsync(treatment => (decimal?)treatment.Price) ?? 0m;
        return View(await treatments.OrderByDescending(treatment => treatment.Appointment!.AppointmentDate).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadAppointmentsAsync();
        return View(new Treatment());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("AppointmentId,TreatmentName,Price")] Treatment treatment)
    {
        if (!await _context.Appointments.AnyAsync(appointment => appointment.AppointmentId == treatment.AppointmentId))
        {
            ModelState.AddModelError(nameof(treatment.AppointmentId), "Choose an existing appointment.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAppointmentsAsync(treatment.AppointmentId);
            return View(treatment);
        }

        _context.Treatments.Add(treatment);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Treatment added to the visit.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var treatment = await _context.Treatments.FindAsync(id);
        if (treatment is null)
        {
            return NotFound();
        }

        await LoadAppointmentsAsync(treatment.AppointmentId);
        return View(treatment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("TreatmentId,AppointmentId,TreatmentName,Price")] Treatment treatment)
    {
        if (id != treatment.TreatmentId)
        {
            return NotFound();
        }

        if (!await _context.Appointments.AnyAsync(appointment => appointment.AppointmentId == treatment.AppointmentId))
        {
            ModelState.AddModelError(nameof(treatment.AppointmentId), "Choose an existing appointment.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAppointmentsAsync(treatment.AppointmentId);
            return View(treatment);
        }

        _context.Update(treatment);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Treatment details saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var treatment = await _context.Treatments.AsNoTracking()
            .Include(existing => existing.Appointment).ThenInclude(appointment => appointment!.Pet)
            .FirstOrDefaultAsync(existing => existing.TreatmentId == id);
        return treatment is null ? NotFound() : View(treatment);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var treatment = await _context.Treatments.FindAsync(id);
        if (treatment is null)
        {
            return NotFound();
        }

        _context.Treatments.Remove(treatment);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Treatment removed from the visit.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadAppointmentsAsync(int? selectedAppointment = null)
    {
        var appointments = await _context.Appointments.AsNoTracking()
            .Include(appointment => appointment.Pet)
            .OrderByDescending(appointment => appointment.AppointmentDate)
            .ToListAsync();
        ViewBag.AppointmentId = new SelectList(appointments.Select(appointment => new SelectListItem
        {
            Value = appointment.AppointmentId.ToString(),
            Text = $"{appointment.AppointmentDate:MMM d, yyyy · h:mm tt} — {appointment.Pet?.Name}"
        }), "Value", "Text", selectedAppointment);
    }
}
