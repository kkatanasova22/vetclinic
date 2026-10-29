using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly VetClinicDbContext _context;

    public DashboardController(VetClinicDbContext context) => _context = context;

    // Клиентът получава обобщение само за своите животни и часове.
    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;
        var owners = _context.Owners.AsNoTracking().AsQueryable();
        var pets = _context.Pets.AsNoTracking().AsQueryable();
        var appointmentRows = _context.Appointments.AsNoTracking().AsQueryable();
        if (User.IsInRole("Client"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            owners = owners.Where(owner => owner.ClientProfile!.UserId == userId);
            pets = pets.Where(pet => pet.Owner!.ClientProfile!.UserId == userId);
            appointmentRows = appointmentRows.Where(appointment => appointment.Pet!.Owner!.ClientProfile!.UserId == userId);
        }

        var appointments = _context.Appointments
            .AsNoTracking()
            .Include(appointment => appointment.Pet)
                .ThenInclude(pet => pet!.Owner)
            .Include(appointment => appointment.Veterinarian)
            .Where(appointment => appointment.AppointmentDate >= now.AddHours(-2) && appointment.Status != "Cancelled")
            .OrderBy(appointment => appointment.AppointmentDate);
        if (User.IsInRole("Client"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            appointments = appointments.Where(appointment => appointment.Pet!.Owner!.ClientProfile!.UserId == userId)
                .OrderBy(appointment => appointment.AppointmentDate);
        }

        var viewModel = new DashboardViewModel
        {
            OwnerCount = await owners.CountAsync(),
            PetCount = await pets.CountAsync(),
            VeterinarianCount = await _context.Veterinarians.CountAsync(),
            UpcomingAppointmentCount = await appointmentRows
                .CountAsync(appointment => appointment.AppointmentDate >= now && appointment.Status != "Cancelled"),
            Appointments = await appointments.Take(6).ToListAsync(),
            NextAppointment = await appointments.FirstOrDefaultAsync()
        };

        return View(viewModel);
    }
}
