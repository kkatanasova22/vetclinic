namespace WebApplication1.Controllers;
using Microsoft.AspNetCore.Authorization;

[Authorize(Roles = "Admin")]
public class VeterinariansController : Controller
{
}
