using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[Authorize(Roles = "Admin,Receptionist")]
public class OwnersController : Controller
{
    private const int PageSize = 10;
    private readonly VetClinicDbContext _context;

    public OwnersController(VetClinicDbContext context) => _context = context;

    // Търсим и подреждаме собствениците в заявката към базата.
    public async Task<IActionResult> Index(string? search, string? sort, int page = 1)
    {
        var owners = _context.Owners.AsNoTracking().Include(owner => owner.Pets).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            owners = owners.Where(owner => owner.FirstName.Contains(term)
                || owner.LastName.Contains(term)
                || owner.Email.Contains(term)
                || owner.Phone.Contains(term));
        }

        page = Math.Max(1, page);
        var total = await owners.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Min(page, totalPages);
        owners = sort == "email"
            ? owners.OrderBy(owner => owner.Email)
            : owners.OrderBy(owner => owner.LastName).ThenBy(owner => owner.FirstName);
        ViewData["Search"] = search;
        ViewData["Sort"] = sort;
        ViewData["OwnerCount"] = await _context.Owners.CountAsync();
        ViewData["PetCount"] = await _context.Pets.CountAsync();
        ViewData["PageInfo"] = new PageInfo { Page = page, TotalPages = totalPages, Search = search, Sort = sort };
        return View(await owners.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync());
    }

    [HttpGet]
    // Показва празна форма за нов собственик.
    public IActionResult Create() => View(new Owner());

    [HttpPost]
    [ValidateAntiForgeryToken]
    // Проверява имейла и записва новия собственик.
    public async Task<IActionResult> Create([Bind("FirstName,LastName,Email,Phone")] Owner owner)
    {
        owner.Email = owner.Email.Trim().ToLowerInvariant();
        if (await _context.Owners.AnyAsync(existing => existing.Email == owner.Email))
        {
            ModelState.AddModelError(nameof(owner.Email), "An owner with this email address already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(owner);
        }

        _context.Owners.Add(owner);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Owner added successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    // Зарежда съществуващия собственик за редакция.
    public async Task<IActionResult> Edit(int id)
    {
        var owner = await _context.Owners.FindAsync(id);
        return owner is null ? NotFound() : View(owner);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // Записва промените само ако имейлът не се ползва от друг собственик.
    public async Task<IActionResult> Edit(int id, [Bind("OwnerId,FirstName,LastName,Email,Phone")] Owner owner)
    {
        if (id != owner.OwnerId)
        {
            return NotFound();
        }

        owner.Email = owner.Email.Trim().ToLowerInvariant();
        if (await _context.Owners.AnyAsync(existing => existing.Email == owner.Email && existing.OwnerId != owner.OwnerId))
        {
            ModelState.AddModelError(nameof(owner.Email), "An owner with this email address already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(owner);
        }

        _context.Update(owner);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Owner details saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    // Показва данните и любимците преди изтриване.
    public async Task<IActionResult> Delete(int id)
    {
        var owner = await _context.Owners
            .AsNoTracking()
            .Include(existing => existing.Pets)
            .FirstOrDefaultAsync(existing => existing.OwnerId == id);
        return owner is null ? NotFound() : View(owner);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    // Не изтрива собственик, към когото още има свързани любимци.
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var owner = await _context.Owners.Include(existing => existing.Pets).FirstOrDefaultAsync(existing => existing.OwnerId == id);
        if (owner is null)
        {
            return NotFound();
        }

        if (owner.Pets.Count > 0)
        {
            TempData["Error"] = "This owner still has pets. Reassign or remove the pets before deleting the owner.";
            return RedirectToAction(nameof(Index));
        }

        _context.Owners.Remove(owner);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Owner removed.";
        return RedirectToAction(nameof(Index));
    }
}
