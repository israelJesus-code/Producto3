using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaLegalPagares.Data;
using SistemaLegalPagares.Models;

namespace SistemaLegalPagares.Controllers.Api;

[ApiController]
[Route("api/v1/clientes")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Abogado")]
public class ClientesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ClientesApiController(ApplicationDbContext context) => _context = context;

    private static ClienteDto ToDto(Cliente c) => new(
        c.Id, c.NombreCompleto, c.CURP, c.INE, c.RFC, c.Telefono, c.Correo, c.Direccion, c.FechaRegistro);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClienteDto>>> GetAll()
    {
        var clientes = await _context.Clientes.OrderBy(c => c.NombreCompleto).ToListAsync();
        return Ok(clientes.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClienteDto>> GetById(int id)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        return cliente is null ? NotFound() : Ok(ToDto(cliente));
    }

    [HttpPost]
    public async Task<ActionResult<ClienteDto>> Create(ClienteWriteDto dto)
    {
        var cliente = new Cliente
        {
            NombreCompleto = dto.NombreCompleto,
            CURP = dto.CURP,
            INE = dto.INE,
            RFC = dto.RFC,
            Telefono = dto.Telefono,
            Correo = dto.Correo,
            Direccion = dto.Direccion,
            FechaRegistro = DateTime.UtcNow,
        };
        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = cliente.Id }, ToDto(cliente));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ClienteWriteDto dto)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();

        cliente.NombreCompleto = dto.NombreCompleto;
        cliente.CURP = dto.CURP;
        cliente.INE = dto.INE;
        cliente.RFC = dto.RFC;
        cliente.Telefono = dto.Telefono;
        cliente.Correo = dto.Correo;
        cliente.Direccion = dto.Direccion;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();

        _context.Clientes.Remove(cliente);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
