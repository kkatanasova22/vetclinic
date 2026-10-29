using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[Authorize(Roles = "Admin")]
public class VeterinariansController : Controller
{
    private const int PageSize = 10;
    private readonly VetClinicDbContext _context;

    public VeterinariansController(VetClinicDbContext context) => _context = context;

    // Търсим по лекар или специалност и връщаме само избраната страница.
    public async Task<IActionResult> Index(string? search, string? sort, int page = 1)
    {
        var veterinarians = _context.Veterinarians.AsNoTracking().Include(vet => vet.Appointments).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            veterinarians = veterinarians.Where(vet => vet.FirstName.Contains(term)
                || vet.LastName.Contains(term)
                || (vet.Specialty != null && vet.Specialty.Contains(term)));
        }

        page = Math.Max(1, page);
        var total = await veterinarians.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Min(page, totalPages);
        veterinarians = sort == "specialty"
            ? veterinarians.OrderBy(vet => vet.Specialty).ThenBy(vet => vet.LastName)
            : veterinarians.OrderBy(vet => vet.LastName).ThenBy(vet => vet.FirstName);
        ViewData["Search"] = search;
        ViewData["Sort"] = sort;
        ViewData["VeterinarianCount"] = await _context.Veterinarians.CountAsync();
        ViewData["PageInfo"] = new PageInfo { Page = page, TotalPages = totalPages, Search = search, Sort = sort };
        return View(await veterinarians.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync());
    }

    [HttpGet]
    // Показва празна форма за нов лекар.
    public IActionResult Create() => View(new Veterinarian());

    [HttpPost]
    [ValidateAntiForgeryToken]
    // Проверява задължителните полета и записва лекаря.
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
    // Зарежда лекаря за редакция.
    public async Task<IActionResult> Edit(int id)
    {
        var veterinarian = await _context.Veterinarians.FindAsync(id);
        return veterinarian is null ? NotFound() : View(veterinarian);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // Запазва новите данни за лекаря.
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
    // Показва лекаря преди изтриване.
    public async Task<IActionResult> Delete(int id)
    {
        var veterinarian = await _context.Veterinarians.AsNoTracking()
            .FirstOrDefaultAsync(vet => vet.VeterinarianId == id);
        return veterinarian is null ? NotFound() : View(veterinarian);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    // Запазва историята на часовете и спира изтриване на използван лекар.
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
