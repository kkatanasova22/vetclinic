using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data;

public static class SeedData
{
    // Добавя начални данни и роли само ако още липсват.
    public static async Task InitializeAsync(
        IServiceProvider services,
        bool isDevelopment,
        IConfiguration configuration)
    {
        var db = services.GetRequiredService<VetClinicDbContext>();

        if (!await db.Owners.AnyAsync())
        {
            var owners = new[]
            {
                new Owner
                {
                    FirstName = "Иван", LastName = "Петров", Phone = "0888123456",
                    Email = "ivan@gmail.com",
                    Pets = new List<Pet>
                    {
                        new() { Name = "Рекс", Species = "Dog", Breed = "Немска овчарка", Sex = "Male", BirthDate = new DateTime(2021, 5, 10) }
                    }
                },
                new Owner
                {
                    FirstName = "Мария", LastName = "Георгиева", Phone = "0888234567",
                    Email = "maria@gmail.com",
                    Pets = new List<Pet>
                    {
                        new() { Name = "Мая", Species = "Cat", Breed = "Сиамска", Sex = "Female", BirthDate = new DateTime(2022, 3, 15) }
                    }
                }
            };

            db.Owners.AddRange(owners);
            db.Veterinarians.AddRange(
                new Veterinarian { FirstName = "Николай", LastName = "Иванов", Specialty = "Хирургия" },
                new Veterinarian { FirstName = "Елена", LastName = "Тодорова", Specialty = "Терапия" });
            await db.SaveChangesAsync();

            var pets = await db.Pets.OrderBy(pet => pet.PetId).ToListAsync();
            var veterinarians = await db.Veterinarians.OrderBy(vet => vet.VeterinarianId).ToListAsync();
            var appointments = new[]
            {
                new Appointment
                {
                    PetId = pets[0].PetId, VeterinarianId = veterinarians[0].VeterinarianId,
                    AppointmentDate = DateTime.Now.AddHours(2), Diagnosis = "Годишен преглед", Status = "Confirmed",
                    Treatments = new List<Treatment> { new() { TreatmentName = "Ваксинация", Price = 40m } }
                },
                new Appointment
                {
                    PetId = pets[1].PetId, VeterinarianId = veterinarians[1].VeterinarianId,
                    AppointmentDate = DateTime.Now.AddDays(1).Date.AddHours(14).AddMinutes(30),
                    Diagnosis = "Настинка", Status = "Confirmed",
                    Treatments = new List<Treatment> { new() { TreatmentName = "Медикаментозно лечение", Price = 25m } }
                }
            };
            db.Appointments.AddRange(appointments);
            await db.SaveChangesAsync();
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var roleName in new[] { "Admin", "Veterinarian", "Receptionist", "Client" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        if (!isDevelopment)
        {
            return;
        }

        // Администратор се създава само с настройки от средата, без парола в кода.
        var email = configuration["BootstrapAdmin:Email"];
        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(admin, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}
