using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data;
using WebApplication1.Models;
using Microsoft.AspNetCore.Authorization;
namespace WebApplication1.Controllers;

[Authorize(Roles = "Admin")]
public class OwnersController : Controller
{
    private readonly VetClinicDbContext _context;

    public OwnersController(VetClinicDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View(_context.Owners.ToList());
    }
    public IActionResult Details(int id)
    {
        var owner = _context.Owners.Find(id);

        if (owner == null)
        {
            return NotFound();
        }

        return View(owner);
    }
    public IActionResult Index()
    {
        return View(_context.Owners.ToList());
    }

    public IActionResult Details(int id)
    {
        var owner = _context.Owners.Find(id);

        if (owner == null)
        {
            return NotFound();
        }

        return View(owner);
    }

    public IActionResult Create()
    {
        return View();
    }
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(Owner owner)
    {
        if (ModelState.IsValid)
        {
            _context.Owners.Add(owner);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        return View(owner);
    }

    public IActionResult Edit(int id)
    {
        var owner = _context.Owners.Find(id);

        if (owner == null)
        {
            return NotFound();
        }

        return View(owner);
    }
    [HttpPost]
    public IActionResult Edit(Owner owner)
    {
        if (ModelState.IsValid)
        {
            _context.Update(owner);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        return View(owner);
    }

    public IActionResult Delete(int id)
    {
        var owner = _context.Owners.Find(id);

        if (owner == null)
        {
            return NotFound();
        }

        return View(owner);
    }

    [HttpPost, ActionName("Delete")]
    public IActionResult DeleteConfirmed(int id)
    {
        var owner = _context.Owners.Find(id);

        if (owner != null)
        {
            _context.Owners.Remove(owner);
            _context.SaveChanges();
        }

        return RedirectToAction(nameof(Index));
    }
} 
