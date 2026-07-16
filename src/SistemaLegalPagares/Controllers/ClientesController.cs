using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaLegalPagares.Data;
using SistemaLegalPagares.Models;

namespace SistemaLegalPagares.Controllers;

[Authorize(Roles = "Admin,Abogado")]
public class ClientesController : Controller
{
    private readonly ApplicationDbContext _context;

    public ClientesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Clientes.OrderByDescending(c => c.FechaRegistro).ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var cliente = await _context.Clientes
            .Include(c => c.Expedientes)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (cliente is null) return NotFound();
        return View(cliente);
    }

    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("NombreCompleto,CURP,INE,RFC,Telefono,Correo,Direccion")] Cliente cliente)
    {
        if (ModelState.IsValid)
        {
            cliente.FechaRegistro = DateTime.UtcNow;
            _context.Add(cliente);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(cliente);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();
        return View(cliente);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,
        [Bind("Id,NombreCompleto,CURP,INE,RFC,Telefono,Correo,Direccion,FechaRegistro")] Cliente cliente)
    {
        if (id != cliente.Id) return NotFound();

        if (ModelState.IsValid)
        {
            _context.Update(cliente);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(cliente);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();
        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id);
        if (cliente is null) return NotFound();
        return View(cliente);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente is not null)
        {
            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
