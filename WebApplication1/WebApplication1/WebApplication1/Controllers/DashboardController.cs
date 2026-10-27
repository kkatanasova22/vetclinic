using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly VetClinicDbContext _context;

    public DashboardController(VetClinicDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;
        var appointments = _context.Appointments
            .AsNoTracking()
            .Include(appointment => appointment.Pet)
                .ThenInclude(pet => pet!.Owner)
            .Include(appointment => appointment.Veterinarian)
            .Where(appointment => appointment.AppointmentDate >= now.AddHours(-2) && appointment.Status != "Cancelled")
            .OrderBy(appointment => appointment.AppointmentDate);

        var viewModel = new DashboardViewModel
        {
            OwnerCount = await _context.Owners.CountAsync(),
            PetCount = await _context.Pets.CountAsync(),
            VeterinarianCount = await _context.Veterinarians.CountAsync(),
            UpcomingAppointmentCount = await _context.Appointments
                .CountAsync(appointment => appointment.AppointmentDate >= now && appointment.Status != "Cancelled"),
            Appointments = await appointments.Take(6).ToListAsync(),
            NextAppointment = await appointments.FirstOrDefaultAsync()
        };

        return View(viewModel);
    }
}
