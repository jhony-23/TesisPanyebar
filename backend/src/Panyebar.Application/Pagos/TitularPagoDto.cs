namespace Panyebar.Application.Pagos;

public sealed record TitularPagoDto(
    string Tipo,
    int Id,
    string Nombre,
    string? Nis);
