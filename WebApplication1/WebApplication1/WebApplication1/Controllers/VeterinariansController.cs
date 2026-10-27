using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[Authorize(Roles = "Admin")]
public class VeterinariansController : Controller
{
    private readonly VetClinicDbContext _context;

    public VeterinariansController(VetClinicDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? search)
    {
        var veterinarians = _context.Veterinarians.AsNoTracking().Include(vet => vet.Appointments).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            veterinarians = veterinarians.Where(vet => vet.FirstName.Contains(term)
                || vet.LastName.Contains(term)
                || (vet.Specialty != null && vet.Specialty.Contains(term)));
        }

        ViewData["Search"] = search;
        ViewData["VeterinarianCount"] = await _context.Veterinarians.CountAsync();
        return View(await veterinarians.OrderBy(vet => vet.LastName).ThenBy(vet => vet.FirstName).ToListAsync());
    }

    [HttpGet]
    public IActionResult Create() => View(new Veterinarian());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FirstName,LastName,Specialty")] Veterinarian veterinarian)
    {
        if (!ModelState.IsValid)
        {
            return View(veterinarian);
        }

        _context.Veterinarians.Add(veterinarian);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Veterinarian added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var veterinarian = await _context.Veterinarians.FindAsync(id);
        return veterinarian is null ? NotFound() : View(veterinarian);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("VeterinarianId,FirstName,LastName,Specialty")] Veterinarian veterinarian)
    {
        if (id != veterinarian.VeterinarianId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(veterinarian);
        }

        _context.Update(veterinarian);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Veterinarian details saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var veterinarian = await _context.Veterinarians.AsNoTracking()
            .FirstOrDefaultAsync(vet => vet.VeterinarianId == id);
        return veterinarian is null ? NotFound() : View(veterinarian);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var veterinarian = await _context.Veterinarians.FindAsync(id);
        if (veterinarian is null)
        {
            return NotFound();
        }

        if (await _context.Appointments.AnyAsync(appointment => appointment.VeterinarianId == id))
        {
            TempData["Error"] = "This veterinarian has appointment history and cannot be removed.";
            return RedirectToAction(nameof(Index));
        }

        _context.Veterinarians.Remove(veterinarian);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Veterinarian removed.";
        return RedirectToAction(nameof(Index));
    }
}
