using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaLegalPagares.Data;
using SistemaLegalPagares.Models;

namespace SistemaLegalPagares.Controllers.Api;

[ApiController]
[Route("api/v1/expedientes")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Abogado")]
public class ExpedientesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ExpedientesApiController(ApplicationDbContext context) => _context = context;

    private static ExpedienteDto ToDto(Expediente e) => new(
        e.Id, e.NumeroExpediente, e.ClienteId, e.Cliente?.NombreCompleto, e.Observaciones, e.FechaCreacion, e.Pagares.Count);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpedienteDto>>> GetAll()
    {
        var expedientes = await _context.Expedientes
            .Include(e => e.Cliente)
            .Include(e => e.Pagares)
            .OrderByDescending(e => e.FechaCreacion)
            .ToListAsync();
        return Ok(expedientes.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExpedienteDto>> GetById(int id)
    {
        var expediente = await _context.Expedientes
            .Include(e => e.Cliente)
            .Include(e => e.Pagares)
            .FirstOrDefaultAsync(e => e.Id == id);
        return expediente is null ? NotFound() : Ok(ToDto(expediente));
    }

    [HttpPost]
    public async Task<ActionResult<ExpedienteDto>> Create(ExpedienteWriteDto dto)
    {
        var expediente = new Expediente
        {
            NumeroExpediente = dto.NumeroExpediente,
            ClienteId = dto.ClienteId,
            Observaciones = dto.Observaciones,
            FechaCreacion = DateTime.UtcNow,
        };
        _context.Expedientes.Add(expediente);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = expediente.Id }, ToDto(expediente));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ExpedienteWriteDto dto)
    {
        var expediente = await _context.Expedientes.FindAsync(id);
        if (expediente is null) return NotFound();

        expediente.NumeroExpediente = dto.NumeroExpediente;
        expediente.ClienteId = dto.ClienteId;
        expediente.Observaciones = dto.Observaciones;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var expediente = await _context.Expedientes.FindAsync(id);
        if (expediente is null) return NotFound();

        _context.Expedientes.Remove(expediente);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
