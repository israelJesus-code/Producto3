using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaLegalPagares.Data;
using SistemaLegalPagares.Models;

namespace SistemaLegalPagares.Controllers.Api;

[ApiController]
[Route("api/v1/deudores")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Abogado")]
public class DeudoresApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DeudoresApiController(ApplicationDbContext context) => _context = context;

    private static DeudorDto ToDto(Deudor d) => new(
        d.Id, d.NombreCompleto, d.CURP, d.INE, d.RFC, d.Telefono, d.Correo, d.Direccion, d.Poblacion, d.FechaRegistro);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeudorDto>>> GetAll()
    {
        var deudores = await _context.Deudores.OrderBy(d => d.NombreCompleto).ToListAsync();
        return Ok(deudores.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DeudorDto>> GetById(int id)
    {
        var deudor = await _context.Deudores.FindAsync(id);
        return deudor is null ? NotFound() : Ok(ToDto(deudor));
    }

    [HttpPost]
    public async Task<ActionResult<DeudorDto>> Create(DeudorWriteDto dto)
    {
        var deudor = new Deudor
        {
            NombreCompleto = dto.NombreCompleto,
            CURP = dto.CURP,
            INE = dto.INE,
            RFC = dto.RFC,
            Telefono = dto.Telefono,
            Correo = dto.Correo,
            Direccion = dto.Direccion,
            Poblacion = dto.Poblacion,
            FechaRegistro = DateTime.UtcNow,
        };
        _context.Deudores.Add(deudor);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = deudor.Id }, ToDto(deudor));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, DeudorWriteDto dto)
    {
        var deudor = await _context.Deudores.FindAsync(id);
        if (deudor is null) return NotFound();

        deudor.NombreCompleto = dto.NombreCompleto;
        deudor.CURP = dto.CURP;
        deudor.INE = dto.INE;
        deudor.RFC = dto.RFC;
        deudor.Telefono = dto.Telefono;
        deudor.Correo = dto.Correo;
        deudor.Direccion = dto.Direccion;
        deudor.Poblacion = dto.Poblacion;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deudor = await _context.Deudores.FindAsync(id);
        if (deudor is null) return NotFound();

        _context.Deudores.Remove(deudor);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
