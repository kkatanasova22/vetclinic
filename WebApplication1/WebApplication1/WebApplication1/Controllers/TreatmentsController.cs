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
    private const int PageSize = 10;
    private readonly VetClinicDbContext _context;

    public TreatmentsController(VetClinicDbContext context) => _context = context;

    // Филтърът, сортирането и страниците се изпълняват в LINQ към базата.
    public async Task<IActionResult> Index(string? search, string? sort, int page = 1)
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

        page = Math.Max(1, page);
        var total = await treatments.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Min(page, totalPages);
        treatments = sort switch
        {
            "price" => treatments.OrderBy(treatment => treatment.Price),
            "price-desc" => treatments.OrderByDescending(treatment => treatment.Price),
            _ => treatments.OrderByDescending(treatment => treatment.Appointment!.AppointmentDate)
        };
        ViewData["Search"] = search;
        ViewData["Sort"] = sort;
        ViewData["TreatmentCount"] = await _context.Treatments.CountAsync();
        ViewData["TotalValue"] = await _context.Treatments.SumAsync(treatment => (decimal?)treatment.Price) ?? 0m;
        ViewData["PageInfo"] = new PageInfo { Page = page, TotalPages = totalPages, Search = search, Sort = sort };
        return View(await treatments.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync());
    }

    [HttpGet]
    // Показва форма за процедура към вече записан час.
    public async Task<IActionResult> Create()
    {
        await LoadAppointmentsAsync();
        return View(new Treatment());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // Проверява часа и цената, после записва процедурата.
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
    // Зарежда процедурата и списъка с часове за промяна.
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
    // Запазва коригираните данни след повторна проверка.
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
    // Показва данните за процедурата преди изтриване.
    public async Task<IActionResult> Delete(int id)
    {
        var treatment = await _context.Treatments.AsNoTracking()
            .Include(existing => existing.Appointment).ThenInclude(appointment => appointment!.Pet)
            .FirstOrDefaultAsync(existing => existing.TreatmentId == id);
        return treatment is null ? NotFound() : View(treatment);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    // Премахва само избраната процедура.
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
