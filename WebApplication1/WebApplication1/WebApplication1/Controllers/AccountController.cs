using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly VetClinicDbContext _context;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        VetClinicDbContext context)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
    }

    [AllowAnonymous]
    [HttpGet]
    // Показва формата за вход и връща вече влезлия потребител към началото.
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    // Проверява паролата чрез Identity, който я пази като хеш.
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        var result = user is null
            ? Microsoft.AspNetCore.Identity.SignInResult.Failed
            : await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        ModelState.AddModelError(string.Empty, "The email or password is incorrect.");
        return View(model);
    }

    // Всеки нов профил получава ролята Client и собствена карта на стопанин.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        model.Email = model.Email.Trim().ToLowerInvariant();
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _userManager.FindByEmailAsync(model.Email) is not null
            || await _context.Owners.AnyAsync(owner => owner.Email == model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "This email address is already in use.");
            return View(model);
        }

        // Записваме профила и стопанина заедно, за да няма половин регистрация.
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        var owner = new Owner
        {
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            Email = model.Email,
            Phone = model.Phone.Trim()
        };
        _context.Owners.Add(owner);
        await _context.SaveChangesAsync();
        _context.ClientProfiles.Add(new ClientProfile { UserId = user.Id, OwnerId = owner.OwnerId });
        await _context.SaveChangesAsync();

        result = await _userManager.AddToRoleAsync(user, "Client");
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await transaction.CommitAsync();
        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Dashboard");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    // Изтрива сесията за вход.
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    [AllowAnonymous]
    public IActionResult Error() => View("~/Views/Shared/Error.cshtml");
}
