namespace WebApplication1.Controllers;
using Microsoft.AspNetCore.Authorization;

[Authorize(Roles = "Admin,Veterinarian")]
public class TreatmentsController : Controller
{
}
