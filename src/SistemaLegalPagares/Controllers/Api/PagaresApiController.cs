using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using SistemaLegalPagares.Data;
using SistemaLegalPagares.Models;
using SistemaLegalPagares.Services.Pdf;

namespace SistemaLegalPagares.Controllers.Api;

[ApiController]
[Route("api/v1/pagares")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Abogado")]
public class PagaresApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PagaresApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private static PagareDto ToDto(Pagare p) => new(
        p.Id, p.ExpedienteId, p.LugarExpedicion, p.FechaExpedicion, p.Acreedor, p.MontoTotal, p.MontoLetra,
        p.FechaVencimiento, p.Beneficiario, p.LugarPagoPagare, p.InteresMoratorio, p.SerieDesde, p.SerieHasta,
        p.UsuarioId, p.Usuario?.NombreCompleto, p.FechaCreacion, p.PagareDeudores.Select(pd => pd.DeudorId).ToList());

    /// <summary>Admin ve todos los pagarés (auditoría); un abogado solo ve los suyos.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PagareDto>>> GetAll()
    {
        var query = _context.Pagares
            .Include(p => p.Usuario)
            .Include(p => p.PagareDeudores)
            .AsQueryable();

        if (!User.IsInRole(DbInitializer.RolAdmin))
        {
            var userId = _userManager.GetUserId(User);
            query = query.Where(p => p.UsuarioId == userId);
        }

        var pagares = await query.OrderByDescending(p => p.FechaCreacion).ToListAsync();
        return Ok(pagares.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PagareDto>> GetById(int id)
    {
        var pagare = await _context.Pagares
            .Include(p => p.Usuario)
            .Include(p => p.PagareDeudores)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pagare is null) return NotFound();
        if (!PuedeAcceder(pagare)) return Forbid();

        return Ok(ToDto(pagare));
    }

    [HttpPost]
    public async Task<ActionResult<PagareDto>> Create(PagareWriteDto dto)
    {
        if (await _context.Expedientes.FindAsync(dto.ExpedienteId) is null)
        {
            return BadRequest(new { message = "El expediente indicado no existe." });
        }

        if (dto.FechaVencimiento.Date < DateTime.UtcNow.Date)
        {
            return BadRequest(new { message = "La fecha de vencimiento no puede ser anterior a hoy." });
        }

        var pagare = new Pagare
        {
            ExpedienteId = dto.ExpedienteId,
            LugarExpedicion = dto.LugarExpedicion,
            FechaExpedicion = dto.FechaExpedicion,
            Acreedor = dto.Acreedor,
            MontoTotal = dto.MontoTotal,
            MontoLetra = dto.MontoLetra,
            FechaVencimiento = dto.FechaVencimiento,
            Beneficiario = dto.Beneficiario,
            LugarPagoPagare = dto.LugarPagoPagare,
            InteresMoratorio = dto.InteresMoratorio,
            SerieDesde = dto.SerieDesde,
            SerieHasta = dto.SerieHasta,
            UsuarioId = _userManager.GetUserId(User)!,
            FechaCreacion = DateTime.UtcNow,
        };

        foreach (var deudorId in (dto.DeudorIds ?? new List<int>()).Distinct())
        {
            pagare.PagareDeudores.Add(new PagareDeudor { DeudorId = deudorId });
        }

        _context.Pagares.Add(pagare);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = pagare.Id }, ToDto(pagare));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var pagare = await _context.Pagares.FindAsync(id);
        if (pagare is null) return NotFound();
        if (!PuedeAcceder(pagare)) return Forbid();

        _context.Pagares.Remove(pagare);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Genera y descarga el PDF del pagaré (mismo servicio QuestPDF que usa la UI MVC).</summary>
    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> Pdf(int id)
    {
        var pagare = await _context.Pagares
            .Include(p => p.Expediente)
            .Include(p => p.PagareDeudores).ThenInclude(pd => pd.Deudor)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pagare is null) return NotFound();
        if (!PuedeAcceder(pagare)) return Forbid();

        var deudores = pagare.PagareDeudores.Select(pd => pd.Deudor!).ToList();
        var bytes = new PagarePdfDocument(pagare, deudores).GeneratePdf();
        return File(bytes, "application/pdf", $"Pagare-{pagare.Id}.pdf");
    }

    private bool PuedeAcceder(Pagare pagare)
    {
        if (User.IsInRole(DbInitializer.RolAdmin)) return true;
        return pagare.UsuarioId == _userManager.GetUserId(User);
    }
}
