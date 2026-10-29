using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[Authorize(Roles = "Admin,Receptionist,Veterinarian,Client")]
public class FilesController : Controller
{
    private readonly VetClinicDbContext _context;
    private readonly FileStorageService _storage;

    public FilesController(VetClinicDbContext context, FileStorageService storage)
    {
        _context = context;
        _storage = storage;
    }

    // Файл се връща само ако служителят има достъп или е към час на този клиент.
    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var file = await FindAccessibleFileAsync(id);
        if (file is null || !IsSafeStoredName(file.StoredFileName))
        {
            return NotFound();
        }

        var path = _storage.GetPath(file.StoredFileName);
        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "no-store";
        return PhysicalFile(path, file.ContentType, file.OriginalFileName, enableRangeProcessing: true);
    }

    [HttpGet]
    public async Task<IActionResult> Preview(int id)
    {
        var file = await FindAccessibleFileAsync(id);
        if (file?.ThumbnailFileName is null || !IsSafeStoredName(file.ThumbnailFileName))
        {
            return NotFound();
        }

        var path = _storage.GetPath(file.ThumbnailFileName);
        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "no-store";
        return PhysicalFile(path, "image/jpeg");
    }

    private async Task<ClinicFile?> FindAccessibleFileAsync(int id)
    {
        var file = await _context.ClinicFiles.AsNoTracking()
            .Include(item => item.Appointment).ThenInclude(appointment => appointment!.Pet)
                .ThenInclude(pet => pet!.Owner).ThenInclude(owner => owner!.ClientProfile)
            .FirstOrDefaultAsync(item => item.ClinicFileId == id);
        if (file is null)
        {
            return null;
        }

        if (!User.IsInRole("Client") || User.IsInRole("Admin")
            || User.IsInRole("Receptionist") || User.IsInRole("Veterinarian"))
        {
            return file;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return file.Appointment?.Pet?.Owner?.ClientProfile?.UserId == userId ? file : null;
    }

    private static bool IsSafeStoredName(string storedName) =>
        !string.IsNullOrWhiteSpace(storedName)
        && Path.GetFileName(storedName) == storedName
        && storedName.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '.');
}
