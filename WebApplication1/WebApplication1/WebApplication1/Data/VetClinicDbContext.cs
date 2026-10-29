using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data;

// Описва таблиците и връзките, които EF Core пази в базата.
public class VetClinicDbContext : IdentityDbContext<ApplicationUser>
{
    public VetClinicDbContext(DbContextOptions<VetClinicDbContext> options) : base(options)
    {
    }

    // Всяко DbSet съответства на таблица в базата.
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<Veterinarian> Veterinarians => Set<Veterinarian>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Treatment> Treatments => Set<Treatment>();
    public DbSet<ClientProfile> ClientProfiles => Set<ClientProfile>();
    public DbSet<ClinicFile> ClinicFiles => Set<ClinicFile>();

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

        // Ограниченията пазят връзките и забраняват невалидни цени и размери на файлове.
        builder.Entity<ClientProfile>().HasIndex(profile => profile.OwnerId).IsUnique();
        builder.Entity<ClientProfile>().HasOne(profile => profile.User).WithOne()
            .HasForeignKey<ClientProfile>(profile => profile.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ClientProfile>().HasOne(profile => profile.Owner).WithOne(owner => owner.ClientProfile)
            .HasForeignKey<ClientProfile>(profile => profile.OwnerId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ClinicFile>().HasIndex(file => file.StoredFileName).IsUnique();
        builder.Entity<ClinicFile>().ToTable(table =>
            table.HasCheckConstraint("CK_ClinicFiles_Size", "\"SizeBytes\" > 0 AND \"SizeBytes\" <= 10485760"));
        builder.Entity<ClinicFile>().HasOne(file => file.Appointment).WithMany(appointment => appointment.Files)
            .HasForeignKey(file => file.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ClinicFile>().HasOne(file => file.UploadedByUser).WithMany()
            .HasForeignKey(file => file.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);

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
