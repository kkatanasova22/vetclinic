namespace WebApplication1.Models;

// Данните за картите и списъка на началната страница.
public class DashboardViewModel
{
    public int OwnerCount { get; set; }
    public int PetCount { get; set; }
    public int VeterinarianCount { get; set; }
    public int UpcomingAppointmentCount { get; set; }
    public IReadOnlyList<Appointment> Appointments { get; set; } = Array.Empty<Appointment>();
    public Appointment? NextAppointment { get; set; }
}
