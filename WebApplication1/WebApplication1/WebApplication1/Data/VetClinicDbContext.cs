using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data;

public class VetClinicDbContext : IdentityDbContext<ApplicationUser>
{
    public VetClinicDbContext(DbContextOptions<VetClinicDbContext> options) : base(options)
    {
    }

    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<Veterinarian> Veterinarians => Set<Veterinarian>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Treatment> Treatments => Set<Treatment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Owner>().HasIndex(owner => owner.Email).IsUnique();
        builder.Entity<Pet>().HasIndex(pet => pet.OwnerId);
        builder.Entity<Appointment>().HasIndex(appointment => appointment.AppointmentDate);
        builder.Entity<Appointment>().HasIndex(appointment => appointment.PetId);
        builder.Entity<Appointment>().HasIndex(appointment => appointment.VeterinarianId);
        builder.Entity<Treatment>().Property(treatment => treatment.Price).HasPrecision(10, 2);
        builder.Entity<Treatment>().ToTable(table =>
            table.HasCheckConstraint("CK_Treatments_Price_NonNegative", "CAST(\"Price\" AS NUMERIC) >= 0"));

        builder.Entity<Pet>()
            .HasOne(pet => pet.Owner)
            .WithMany(owner => owner.Pets)
            .HasForeignKey(pet => pet.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Appointment>()
            .HasOne(appointment => appointment.Pet)
            .WithMany(pet => pet.Appointments)
            .HasForeignKey(appointment => appointment.PetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Appointment>()
            .HasOne(appointment => appointment.Veterinarian)
            .WithMany(veterinarian => veterinarian.Appointments)
            .HasForeignKey(appointment => appointment.VeterinarianId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Treatment>()
            .HasOne(treatment => treatment.Appointment)
            .WithMany(appointment => appointment.Treatments)
            .HasForeignKey(treatment => treatment.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
