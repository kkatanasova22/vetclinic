using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[Authorize(Roles = "Admin,Receptionist")]
public class AppointmentsController : Controller
{
    private static readonly string[] StatusOptions = ["Confirmed", "In progress", "Completed", "Cancelled"];
    private readonly VetClinicDbContext _context;

    public AppointmentsController(VetClinicDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? search)
    {
        var appointments = _context.Appointments.AsNoTracking()
            .Include(appointment => appointment.Pet).ThenInclude(pet => pet!.Owner)
            .Include(appointment => appointment.Veterinarian)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            appointments = appointments.Where(appointment => appointment.Pet!.Name.Contains(term)
                || appointment.Pet.Owner!.LastName.Contains(term)
                || appointment.Veterinarian!.LastName.Contains(term)
                || (appointment.Diagnosis != null && appointment.Diagnosis.Contains(term)));
        }

        ViewData["Search"] = search;
        ViewData["AppointmentCount"] = await _context.Appointments.CountAsync();
        return View(await appointments.OrderBy(appointment => appointment.AppointmentDate).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadFormOptionsAsync();
        return View(new Appointment { AppointmentDate = DateTime.Now.AddHours(1), Status = "Confirmed" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("PetId,VeterinarianId,AppointmentDate,Diagnosis,Status")] Appointment appointment)
    {
        ValidateStatus(appointment.Status);
        await ValidateReferencesAsync(appointment);
        if (!ModelState.IsValid)
        {
            await LoadFormOptionsAsync(appointment.PetId, appointment.VeterinarianId, appointment.Status);
            return View(appointment);
        }

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Appointment scheduled.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment is null)
        {
            return NotFound();
        }

        await LoadFormOptionsAsync(appointment.PetId, appointment.VeterinarianId, appointment.Status);
        return View(appointment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("AppointmentId,PetId,VeterinarianId,AppointmentDate,Diagnosis,Status")] Appointment appointment)
    {
        if (id != appointment.AppointmentId)
        {
            return NotFound();
        }

        ValidateStatus(appointment.Status);
        await ValidateReferencesAsync(appointment);
        if (!ModelState.IsValid)
        {
            await LoadFormOptionsAsync(appointment.PetId, appointment.VeterinarianId, appointment.Status);
            return View(appointment);
        }

        _context.Update(appointment);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Appointment updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var appointment = await _context.Appointments.AsNoTracking()
            .Include(existing => existing.Pet).ThenInclude(pet => pet!.Owner)
            .Include(existing => existing.Veterinarian)
            .FirstOrDefaultAsync(existing => existing.AppointmentId == id);
        return appointment is null ? NotFound() : View(appointment);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment is null)
        {
            return NotFound();
        }

        if (await _context.Treatments.AnyAsync(treatment => treatment.AppointmentId == id))
        {
            TempData["Error"] = "This appointment has recorded treatments and cannot be removed.";
            return RedirectToAction(nameof(Index));
        }

        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Appointment removed.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadFormOptionsAsync(int? selectedPet = null, int? selectedVeterinarian = null, string? selectedStatus = null)
    {
        var pets = await _context.Pets.AsNoTracking().Include(pet => pet.Owner).OrderBy(pet => pet.Name).ToListAsync();
        ViewBag.PetId = new SelectList(pets.Select(pet => new SelectListItem
        {
            Value = pet.PetId.ToString(),
            Text = $"{pet.Name} · {pet.Owner?.FirstName} {pet.Owner?.LastName}"
        }), "Value", "Text", selectedPet);

        var veterinarians = await _context.Veterinarians.AsNoTracking()
            .OrderBy(vet => vet.LastName).ThenBy(vet => vet.FirstName).ToListAsync();
        ViewBag.VeterinarianId = new SelectList(veterinarians.Select(vet => new SelectListItem
        {
            Value = vet.VeterinarianId.ToString(),
            Text = $"Dr. {vet.FirstName} {vet.LastName} · {vet.Specialty}"
        }), "Value", "Text", selectedVeterinarian);
        ViewBag.Status = new SelectList(StatusOptions, selectedStatus ?? "Confirmed");
    }

    private void ValidateStatus(string status)
    {
        if (!StatusOptions.Contains(status, StringComparer.Ordinal))
        {
            ModelState.AddModelError(nameof(Appointment.Status), "Choose a valid appointment status.");
        }
    }

    private async Task ValidateReferencesAsync(Appointment appointment)
    {
        if (!await _context.Pets.AnyAsync(pet => pet.PetId == appointment.PetId))
        {
            ModelState.AddModelError(nameof(Appointment.PetId), "Choose an existing patient.");
        }

        if (!await _context.Veterinarians.AnyAsync(veterinarian => veterinarian.VeterinarianId == appointment.VeterinarianId))
        {
            ModelState.AddModelError(nameof(Appointment.VeterinarianId), "Choose an existing veterinarian.");
        }
    }
}
