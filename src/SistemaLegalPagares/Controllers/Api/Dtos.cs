namespace SistemaLegalPagares.Controllers.Api;

public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, DateTime ExpiresAtUtc, string NombreCompleto, IList<string> Roles);

public record ClienteDto(int Id, string NombreCompleto, string? CURP, string? INE, string? RFC, string? Telefono, string? Correo, string? Direccion, DateTime FechaRegistro);
public record ClienteWriteDto(string NombreCompleto, string? CURP, string? INE, string? RFC, string? Telefono, string? Correo, string? Direccion);

public record DeudorDto(int Id, string NombreCompleto, string? CURP, string? INE, string? RFC, string? Telefono, string? Correo, string? Direccion, string? Poblacion, DateTime FechaRegistro);
public record DeudorWriteDto(string NombreCompleto, string? CURP, string? INE, string? RFC, string? Telefono, string? Correo, string? Direccion, string? Poblacion);

public record ExpedienteDto(int Id, string NumeroExpediente, int? ClienteId, string? ClienteNombre, string? Observaciones, DateTime FechaCreacion, int TotalPagares);
public record ExpedienteWriteDto(string NumeroExpediente, int? ClienteId, string? Observaciones);

public record PagareDto(
    int Id, int ExpedienteId, string LugarExpedicion, DateTime FechaExpedicion, string Acreedor,
    decimal MontoTotal, string MontoLetra, DateTime FechaVencimiento, string? Beneficiario,
    string? LugarPagoPagare, decimal InteresMoratorio, int SerieDesde, int SerieHasta,
    string UsuarioId, string? UsuarioNombre, DateTime FechaCreacion, IList<int> DeudorIds);

public record PagareWriteDto(
    int ExpedienteId, string LugarExpedicion, DateTime FechaExpedicion, string Acreedor,
    decimal MontoTotal, string MontoLetra, DateTime FechaVencimiento, string? Beneficiario,
    string? LugarPagoPagare, decimal InteresMoratorio, int SerieDesde, int SerieHasta,
    IList<int>? DeudorIds);
