namespace Panyebar.Application.Pagos;

public sealed record PagoDetalleDto(
    int Id,
    string NumeroComprobante,
    decimal Monto,
    DateTime Fecha,
    string Concepto,
    Panyebar.Domain.Enums.EstadoPago Estado,
    int UsuarioAdministrativoId,
    string UsuarioAdministrativo,
    TitularPagoDto Titular,
    IReadOnlyList<ObligacionPagoDto> Obligaciones);
