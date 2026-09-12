using Panyebar.Domain.Enums;

namespace Panyebar.Application.Pagos;

public sealed record PagoDto(
    int Id,
    string NumeroComprobante,
    decimal Monto,
    DateTime Fecha,
    string Concepto,
    EstadoPago Estado,
    int UsuarioAdministrativoId,
    TitularPagoDto Titular);
