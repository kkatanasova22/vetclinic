using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[Authorize(Roles = "Admin,Receptionist,Veterinarian")]
public class PetsController : Controller
{
    private readonly VetClinicDbContext _context;

    public PetsController(VetClinicDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? search)
    {
        var pets = _context.Pets.AsNoTracking().Include(pet => pet.Owner).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            pets = pets.Where(pet => pet.Name.Contains(term)
                || pet.Species.Contains(term)
                || (pet.Breed != null && pet.Breed.Contains(term))
                || pet.Owner!.FirstName.Contains(term)
                || pet.Owner.LastName.Contains(term));
        }

        ViewData["Search"] = search;
        ViewData["PetCount"] = await _context.Pets.CountAsync();
        return View(await pets.OrderBy(pet => pet.Name).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadOwnersAsync();
        return View(new Pet { BirthDate = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Species,Breed,Sex,BirthDate,OwnerId")] Pet pet)
    {
        if (!await _context.Owners.AnyAsync(owner => owner.OwnerId == pet.OwnerId))
        {
            ModelState.AddModelError(nameof(pet.OwnerId), "Choose an existing owner.");
        }

        if (!ModelState.IsValid)
        {
            await LoadOwnersAsync(pet.OwnerId);
            return View(pet);
        }

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Pet profile created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var pet = await _context.Pets.FindAsync(id);
        if (pet is null)
        {
            return NotFound();
        }

        await LoadOwnersAsync(pet.OwnerId);
        return View(pet);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("PetId,Name,Species,Breed,Sex,BirthDate,OwnerId")] Pet pet)
    {
        if (id != pet.PetId)
        {
            return NotFound();
        }

        if (!await _context.Owners.AnyAsync(owner => owner.OwnerId == pet.OwnerId))
        {
            ModelState.AddModelError(nameof(pet.OwnerId), "Choose an existing owner.");
        }

        if (!ModelState.IsValid)
        {
            await LoadOwnersAsync(pet.OwnerId);
            return View(pet);
        }

        _context.Update(pet);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Pet profile saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var pet = await _context.Pets.AsNoTracking().Include(existing => existing.Owner)
            .FirstOrDefaultAsync(existing => existing.PetId == id);
        return pet is null ? NotFound() : View(pet);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var pet = await _context.Pets.FindAsync(id);
        if (pet is null)
        {
            return NotFound();
        }

        if (await _context.Appointments.AnyAsync(appointment => appointment.PetId == id))
        {
            TempData["Error"] = "This pet has appointment history and cannot be removed.";
            return RedirectToAction(nameof(Index));
        }

        _context.Pets.Remove(pet);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Pet profile removed.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadOwnersAsync(int? selectedOwner = null)
    {
        ViewBag.OwnerId = new SelectList(
            await _context.Owners.AsNoTracking().OrderBy(owner => owner.LastName).ThenBy(owner => owner.FirstName).ToListAsync(),
            nameof(Owner.OwnerId),
            nameof(Owner.LastName),
            selectedOwner);
    }
}
