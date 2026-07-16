using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaLegalPagares.Data;
using SistemaLegalPagares.Models;

namespace SistemaLegalPagares.Controllers;

[Authorize(Roles = "Admin,Abogado")]
public class DeudoresController : Controller
{
    private readonly ApplicationDbContext _context;

    public DeudoresController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Deudores.OrderByDescending(d => d.FechaRegistro).ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var deudor = await _context.Deudores.FirstOrDefaultAsync(d => d.Id == id);
        if (deudor is null) return NotFound();
        return View(deudor);
    }

    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("NombreCompleto,CURP,INE,RFC,Telefono,Correo,Direccion,Poblacion")] Deudor deudor)
    {
        if (ModelState.IsValid)
        {
            deudor.FechaRegistro = DateTime.UtcNow;
            _context.Add(deudor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(deudor);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var deudor = await _context.Deudores.FindAsync(id);
        if (deudor is null) return NotFound();
        return View(deudor);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,
        [Bind("Id,NombreCompleto,CURP,INE,RFC,Telefono,Correo,Direccion,Poblacion,FechaRegistro")] Deudor deudor)
    {
        if (id != deudor.Id) return NotFound();

        if (ModelState.IsValid)
        {
            _context.Update(deudor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(deudor);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();
        var deudor = await _context.Deudores.FirstOrDefaultAsync(d => d.Id == id);
        if (deudor is null) return NotFound();
        return View(deudor);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var deudor = await _context.Deudores.FindAsync(id);
        if (deudor is not null)
        {
            _context.Deudores.Remove(deudor);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
