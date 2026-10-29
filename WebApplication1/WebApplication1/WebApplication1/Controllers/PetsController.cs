using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[Authorize(Roles = "Admin,Receptionist,Veterinarian,Client")]
public class PetsController : Controller
{
    private const int PageSize = 10;
    private readonly VetClinicDbContext _context;

    public PetsController(VetClinicDbContext context) => _context = context;

    // Търсим и показваме само любимците, които текущият потребител има право да вижда.
    public async Task<IActionResult> Index(string? search, string? species, string? sort, int page = 1)
    {
        var pets = _context.Pets.AsNoTracking().Include(pet => pet.Owner).AsQueryable();
        if (User.IsInRole("Client"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            pets = pets.Where(pet => pet.Owner!.ClientProfile!.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            pets = pets.Where(pet => pet.Name.Contains(term)
                || pet.Species.Contains(term)
                || (pet.Breed != null && pet.Breed.Contains(term))
                || pet.Owner!.FirstName.Contains(term)
                || pet.Owner.LastName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(species))
        {
            pets = pets.Where(pet => pet.Species == species);
        }

        page = Math.Max(1, page);
        var total = await pets.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Min(page, totalPages);
        pets = sort switch
        {
            "name-desc" => pets.OrderByDescending(pet => pet.Name),
            "species" => pets.OrderBy(pet => pet.Species).ThenBy(pet => pet.Name),
            "date" => pets.OrderBy(pet => pet.BirthDate),
            _ => pets.OrderBy(pet => pet.Name)
        };

        ViewData["Search"] = search;
        ViewData["Species"] = species;
        ViewData["SpeciesOptions"] = await _context.Pets.AsNoTracking().Select(pet => pet.Species).Distinct().OrderBy(name => name).ToListAsync();
        ViewData["Sort"] = sort;
        ViewData["PetCount"] = total;
        ViewData["PageInfo"] = new PageInfo { Page = page, TotalPages = totalPages, Search = search, Category = species, Sort = sort };
        return View(await pets.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync());
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
        if (!await CanUseOwnerAsync(pet.OwnerId))
        {
            ModelState.AddModelError(nameof(pet.OwnerId), "Choose an owner from your account.");
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
        var pet = await FindAccessiblePetAsync(id);
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
        var existing = await FindAccessiblePetAsync(id);
        if (existing is null || id != pet.PetId)
        {
            return NotFound();
        }

        if (!await CanUseOwnerAsync(pet.OwnerId))
        {
            ModelState.AddModelError(nameof(pet.OwnerId), "Choose an owner from your account.");
        }

        if (!ModelState.IsValid)
        {
            await LoadOwnersAsync(pet.OwnerId);
            return View(pet);
        }

        _context.Entry(existing).CurrentValues.SetValues(pet);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Pet profile saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var pet = await FindAccessiblePetAsync(id);
        return pet is null ? NotFound() : View(pet);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var pet = await FindAccessiblePetAsync(id);
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

    private async Task<Pet?> FindAccessiblePetAsync(int id)
    {
        var pets = _context.Pets.Include(pet => pet.Owner).AsQueryable();
        if (User.IsInRole("Client"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            pets = pets.Where(pet => pet.Owner!.ClientProfile!.UserId == userId);
        }

        return await pets.FirstOrDefaultAsync(pet => pet.PetId == id);
    }

    private async Task<bool> CanUseOwnerAsync(int ownerId)
    {
        if (!User.IsInRole("Client"))
        {
            return await _context.Owners.AnyAsync(owner => owner.OwnerId == ownerId);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return await _context.ClientProfiles.AnyAsync(profile => profile.UserId == userId && profile.OwnerId == ownerId);
    }

    private async Task LoadOwnersAsync(int? selectedOwner = null)
    {
        var owners = _context.Owners.AsNoTracking().AsQueryable();
        if (User.IsInRole("Client"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            owners = owners.Where(owner => owner.ClientProfile!.UserId == userId);
        }

        var list = await owners.OrderBy(owner => owner.LastName).ThenBy(owner => owner.FirstName).ToListAsync();
        ViewBag.OwnerId = new SelectList(list, nameof(Owner.OwnerId), nameof(Owner.LastName), selectedOwner);
    }
}
