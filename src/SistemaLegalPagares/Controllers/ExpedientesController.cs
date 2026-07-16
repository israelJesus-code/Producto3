using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaLegalPagares.Data;
using SistemaLegalPagares.Models;

namespace SistemaLegalPagares.Controllers;

[Authorize(Roles = "Admin,Abogado")]
public class ExpedientesController : Controller
{
    private readonly ApplicationDbContext _context;

    public ExpedientesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var expedientes = await _context.Expedientes
            .Include(e => e.Cliente)
            .OrderByDescending(e => e.FechaCreacion)
            .ToListAsync();
        return View(expedientes);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var expediente = await _context.Expedientes
            .Include(e => e.Cliente)
            .Include(e => e.Pagares)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (expediente is null) return NotFound();
        return View(expediente);
    }

    public IActionResult Create()
    {
        ViewBag.Clientes = new SelectList(_context.Clientes.OrderBy(c => c.NombreCompleto), "Id", "NombreCompleto");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("NumeroExpediente,ClienteId,Observaciones")] Expediente expediente)
    {
        if (ModelState.IsValid)
        {
            expediente.FechaCreacion = DateTime.UtcNow;
            _context.Add(expediente);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Clientes = new SelectList(_context.Clientes.OrderBy(c => c.NombreCompleto), "Id", "NombreCompleto", expediente.ClienteId);
        return View(expediente);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var expediente = await _context.Expedientes.FindAsync(id);
        if (expediente is null) return NotFound();
        ViewBag.Clientes = new SelectList(_context.Clientes.OrderBy(c => c.NombreCompleto), "Id", "NombreCompleto", expediente.ClienteId);
        return View(expediente);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,
        [Bind("Id,NumeroExpediente,ClienteId,Observaciones,FechaCreacion")] Expediente expediente)
    {
        if (id != expediente.Id) return NotFound();

        if (ModelState.IsValid)
        {
            _context.Update(expediente);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Clientes = new SelectList(_context.Clientes.OrderBy(c => c.NombreCompleto), "Id", "NombreCompleto", expediente.ClienteId);
        return View(expediente);
    }

    /// <summary>Lista todos los pagarés del expediente con opción de descarga/impresión del PDF.</summary>
    public async Task<IActionResult> ImprimirPagares(int? id)
    {
        if (id is null) return NotFound();
        var expediente = await _context.Expedientes
            .Include(e => e.Cliente)
            .Include(e => e.Pagares)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (expediente is null) return NotFound();
        return View(expediente);
    }
}
