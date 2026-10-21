namespace WebApplication1.Controllers;
using Microsoft.AspNetCore.Authorization;

[Authorize(Roles = "Admin,Receptionist")]
public class AppointmentsController : Controller
{
}
