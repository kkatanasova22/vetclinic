using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[Authorize(Roles = "Admin,Receptionist,Client")]
public class AppointmentsController : Controller
{
    private const int PageSize = 10;
    private static readonly string[] StatusOptions = ["Confirmed", "In progress", "Completed", "Cancelled"];
    private readonly VetClinicDbContext _context;
    private readonly FileStorageService _fileStorage;

    public AppointmentsController(VetClinicDbContext context, FileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    // Клиентът вижда само своите часове; търсенето и филтрите се изпълняват в базата.
    public async Task<IActionResult> Index(string? search, string? status, string? sort, int page = 1)
    {
        var appointments = _context.Appointments.AsNoTracking()
            .Include(appointment => appointment.Pet).ThenInclude(pet => pet!.Owner)
            .Include(appointment => appointment.Veterinarian)
            .Include(appointment => appointment.Files)
            .AsQueryable();

        if (User.IsInRole("Client"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            appointments = appointments.Where(appointment => appointment.Pet!.Owner!.ClientProfile!.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            appointments = appointments.Where(appointment => appointment.Pet!.Name.Contains(term)
                || appointment.Pet.Owner!.LastName.Contains(term)
                || appointment.Veterinarian!.LastName.Contains(term)
                || (appointment.Diagnosis != null && appointment.Diagnosis.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status) && StatusOptions.Contains(status, StringComparer.Ordinal))
        {
            appointments = appointments.Where(appointment => appointment.Status == status);
        }

        page = Math.Max(1, page);
        var total = await appointments.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Min(page, totalPages);
        appointments = sort switch
        {
            "date-desc" => appointments.OrderByDescending(appointment => appointment.AppointmentDate),
            "status" => appointments.OrderBy(appointment => appointment.Status).ThenBy(appointment => appointment.AppointmentDate),
            _ => appointments.OrderBy(appointment => appointment.AppointmentDate)
        };

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        ViewData["Sort"] = sort;
        ViewData["AppointmentCount"] = total;
        ViewData["StatusOptions"] = StatusOptions;
        ViewData["PageInfo"] = new PageInfo { Page = page, TotalPages = totalPages, Search = search, Status = status, Sort = sort };
        return View(await appointments.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadFormOptionsAsync();
        return View(new Appointment { AppointmentDate = DateTime.Now.AddHours(1), Status = "Confirmed" });
    }

    // Записваме часа и прикачените файлове; при невалиден файл махаме незавършения час.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(60_000_000)]
    public async Task<IActionResult> Create(
        [Bind("PetId,VeterinarianId,AppointmentDate,Diagnosis,Status")] Appointment appointment,
        List<IFormFile>? files,
        CancellationToken cancellationToken)
    {
        if (User.IsInRole("Client"))
        {
            appointment.Status = "Confirmed";
        }

        ValidateStatus(appointment.Status);
        await ValidateReferencesAsync(appointment);
        ValidateFileList(files);
        if (!ModelState.IsValid)
        {
            await LoadFormOptionsAsync(appointment.PetId, appointment.VeterinarianId, appointment.Status);
            return View(appointment);
        }

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        var savedFiles = new List<ClinicFile>();
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            foreach (var upload in files ?? [])
            {
                var file = await _fileStorage.SaveAsync(upload, appointment.AppointmentId, userId, cancellationToken);
                savedFiles.Add(file);
                _context.ClinicFiles.Add(file);
            }

            await _context.SaveChangesAsync();
        }
        catch (InvalidDataException exception)
        {
            await _fileStorage.DeleteStoredFilesAsync(savedFiles);
            foreach (var file in savedFiles)
            {
                _context.Entry(file).State = EntityState.Detached;
            }

            _context.Entry(appointment).State = EntityState.Deleted;
            await _context.SaveChangesAsync();
            ModelState.AddModelError("Files", exception.Message);
            await LoadFormOptionsAsync(appointment.PetId, appointment.VeterinarianId, appointment.Status);
            return View(appointment);
        }

        TempData["Success"] = "Appointment scheduled.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Receptionist")]
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

    [Authorize(Roles = "Admin,Receptionist")]
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

    [Authorize(Roles = "Admin,Receptionist")]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var appointment = await _context.Appointments.AsNoTracking()
            .Include(existing => existing.Pet).ThenInclude(pet => pet!.Owner)
            .Include(existing => existing.Veterinarian)
            .FirstOrDefaultAsync(existing => existing.AppointmentId == id);
        return appointment is null ? NotFound() : View(appointment);
    }

    [Authorize(Roles = "Admin,Receptionist")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment is null)
        {
            return NotFound();
        }

        if (await _context.Treatments.AnyAsync(treatment => treatment.AppointmentId == id)
            || await _context.ClinicFiles.AnyAsync(file => file.AppointmentId == id))
        {
            TempData["Error"] = "An appointment with documents or treatments cannot be deleted.";
            return RedirectToAction(nameof(Index));
        }

        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Appointment removed.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadFormOptionsAsync(int? selectedPet = null, int? selectedVeterinarian = null, string? selectedStatus = null)
    {
        var pets = _context.Pets.AsNoTracking().Include(pet => pet.Owner).AsQueryable();
        if (User.IsInRole("Client"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            pets = pets.Where(pet => pet.Owner!.ClientProfile!.UserId == userId);
        }

        var petList = await pets.OrderBy(pet => pet.Name).ToListAsync();
        ViewBag.PetId = new SelectList(petList.Select(pet => new SelectListItem
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
        ViewBag.IsClient = User.IsInRole("Client");
    }

    private void ValidateStatus(string status)
    {
        if (!StatusOptions.Contains(status, StringComparer.Ordinal))
        {
            ModelState.AddModelError(nameof(Appointment.Status), "Choose a valid status.");
        }
    }

    private async Task ValidateReferencesAsync(Appointment appointment)
    {
        var petQuery = _context.Pets.AsQueryable();
        if (User.IsInRole("Client"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            petQuery = petQuery.Where(pet => pet.Owner!.ClientProfile!.UserId == userId);
        }

        if (!await petQuery.AnyAsync(pet => pet.PetId == appointment.PetId))
        {
            ModelState.AddModelError(nameof(Appointment.PetId), "Choose one of your pets from the list.");
        }

        if (!await _context.Veterinarians.AnyAsync(veterinarian => veterinarian.VeterinarianId == appointment.VeterinarianId))
        {
            ModelState.AddModelError(nameof(Appointment.VeterinarianId), "Choose an existing veterinarian.");
        }
    }

    private void ValidateFileList(IReadOnlyCollection<IFormFile>? files)
    {
        if (files is null || files.Count == 0)
        {
            return;
        }

        if (files.Count > 5)
        {
            ModelState.AddModelError("Files", "You can attach up to 5 files to an appointment.");
        }

        if (files.Any(file => file.Length > FileStorageService.MaxFileSize))
        {
            ModelState.AddModelError("Files", "Each file must be 10 MB or smaller.");
        }
    }
}
