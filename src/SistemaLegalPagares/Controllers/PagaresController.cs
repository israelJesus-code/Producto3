using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using SistemaLegalPagares.Data;
using SistemaLegalPagares.Models;
using SistemaLegalPagares.Services.Pdf;

namespace SistemaLegalPagares.Controllers;

[Authorize(Roles = "Admin,Abogado")]
public class PagaresController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PagaresController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    /// <summary>
    /// El administrador ve todos los pagarés del sistema para auditoría (US-11);
    /// un abogado solo ve los pagarés que él mismo creó.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var query = _context.Pagares
            .Include(p => p.Expediente)
            .Include(p => p.Usuario)
            .AsQueryable();

        if (!User.IsInRole(DbInitializer.RolAdmin))
        {
            var userId = _userManager.GetUserId(User);
            query = query.Where(p => p.UsuarioId == userId);
        }

        var pagares = await query.OrderByDescending(p => p.FechaCreacion).ToListAsync();
        return View(pagares);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();

        var pagare = await _context.Pagares
            .Include(p => p.Expediente)
            .Include(p => p.PagareDeudores).ThenInclude(pd => pd.Deudor)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pagare is null) return NotFound();
        if (!await PuedeAccederAsync(pagare)) return Forbid();

        return View(pagare);
    }

    public async Task<IActionResult> Create(int? expedienteId)
    {
        if (expedienteId is null || await _context.Expedientes.FindAsync(expedienteId) is null)
        {
            return NotFound();
        }

        await CargarDeudoresViewBag();
        return View(new Pagare { ExpedienteId = expedienteId.Value, FechaExpedicion = DateTime.UtcNow });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("ExpedienteId,LugarExpedicion,FechaExpedicion,Acreedor,MontoTotal,MontoLetra,FechaVencimiento,Beneficiario,LugarPagoPagare,InteresMoratorio,SerieDesde,SerieHasta,FirmaBase64,FirmaAvalBase64")] Pagare pagare,
        [FromForm] List<int>? deudorIds)
    {
        if (pagare.ExpedienteId <= 0)
        {
            ModelState.AddModelError(string.Empty, "No se recibió el expediente.");
        }
        else if (await _context.Expedientes.FindAsync(pagare.ExpedienteId) is null)
        {
            ModelState.AddModelError(string.Empty, "El expediente indicado no existe.");
        }

        if (pagare.FechaVencimiento.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError(nameof(Pagare.FechaVencimiento), "La fecha de vencimiento no puede ser anterior a hoy.");
        }

        if (ModelState.IsValid)
        {
            pagare.UsuarioId = _userManager.GetUserId(User)!;
            pagare.FechaCreacion = DateTime.UtcNow;

            if (deudorIds is not null)
            {
                foreach (var deudorId in deudorIds.Distinct())
                {
                    pagare.PagareDeudores.Add(new PagareDeudor { DeudorId = deudorId });
                }
            }

            _context.Add(pagare);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = pagare.Id });
        }

        await CargarDeudoresViewBag();
        return View(pagare);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var pagare = await _context.Pagares.Include(p => p.PagareDeudores).FirstOrDefaultAsync(p => p.Id == id);
        if (pagare is null) return NotFound();
        if (!await PuedeAccederAsync(pagare)) return Forbid();

        await CargarDeudoresViewBag(pagare.PagareDeudores.Select(pd => pd.DeudorId).ToList());
        return View(pagare);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,
        [Bind("Id,ExpedienteId,LugarExpedicion,FechaExpedicion,Acreedor,MontoTotal,MontoLetra,FechaVencimiento,Beneficiario,LugarPagoPagare,InteresMoratorio,SerieDesde,SerieHasta,FirmaBase64,FirmaAvalBase64,UsuarioId,FechaCreacion")] Pagare pagare,
        [FromForm] List<int>? deudorIds)
    {
        if (id != pagare.Id) return NotFound();
        if (!await PuedeAccederAsync(pagare)) return Forbid();

        if (pagare.FechaVencimiento.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError(nameof(Pagare.FechaVencimiento), "La fecha de vencimiento no puede ser anterior a hoy.");
        }

        if (ModelState.IsValid)
        {
            var existentes = _context.PagareDeudores.Where(pd => pd.PagareId == id);
            _context.PagareDeudores.RemoveRange(existentes);

            foreach (var deudorId in (deudorIds ?? new List<int>()).Distinct())
            {
                _context.PagareDeudores.Add(new PagareDeudor { PagareId = id, DeudorId = deudorId });
            }

            _context.Update(pagare);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        await CargarDeudoresViewBag(deudorIds);
        return View(pagare);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();
        var pagare = await _context.Pagares.Include(p => p.Expediente).FirstOrDefaultAsync(p => p.Id == id);
        if (pagare is null) return NotFound();
        if (!await PuedeAccederAsync(pagare)) return Forbid();
        return View(pagare);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var pagare = await _context.Pagares.FindAsync(id);
        if (pagare is null) return NotFound();
        if (!await PuedeAccederAsync(pagare)) return Forbid();

        _context.Pagares.Remove(pagare);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Genera el pagaré en PDF (formato Formitec) y lo devuelve como archivo binario.</summary>
    public async Task<IActionResult> Pdf(int id)
    {
        var pagare = await _context.Pagares
            .Include(p => p.Expediente)
            .Include(p => p.PagareDeudores).ThenInclude(pd => pd.Deudor)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pagare is null) return NotFound();
        if (!await PuedeAccederAsync(pagare)) return Forbid();

        var deudores = pagare.PagareDeudores.Select(pd => pd.Deudor!).ToList();
        var document = new PagarePdfDocument(pagare, deudores);
        var bytes = document.GeneratePdf();

        return File(bytes, "application/pdf", $"Pagare-{pagare.Expediente?.NumeroExpediente}-{pagare.Id}.pdf");
    }

    private async Task<bool> PuedeAccederAsync(Pagare pagare)
    {
        if (User.IsInRole(DbInitializer.RolAdmin)) return true;
        var userId = _userManager.GetUserId(User);
        return await Task.FromResult(pagare.UsuarioId == userId);
    }

    private async Task CargarDeudoresViewBag(List<int>? seleccionados = null)
    {
        var deudores = await _context.Deudores
            .OrderBy(d => d.NombreCompleto)
            .Select(d => new { d.Id, d.NombreCompleto, d.CURP })
            .ToListAsync();

        ViewBag.DeudoresJson = JsonSerializer.Serialize(deudores);
        ViewBag.Deudores = new MultiSelectList(await _context.Deudores.OrderBy(d => d.NombreCompleto).ToListAsync(),
            "Id", "NombreCompleto", seleccionados);
    }
}
