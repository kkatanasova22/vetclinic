namespace WebApplication1.Data;

using Microsoft.EntityFrameworkCore;

public class VetClinicDbContext : DbContext
{
    public VetClinicDbContext(
        DbContextOptions<VetClinicDbContext> options)
        : base(options)
    {
    }

    public DbSet<Owner> Owners { get; set; }
    public DbSet<Pet> Pets { get; set; }
    public DbSet<Veterinarian> Veterinarians { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Treatment> Treatments { get; set; }

    public void SaveChanges()
    {
        throw new NotImplementedException();
    }
}