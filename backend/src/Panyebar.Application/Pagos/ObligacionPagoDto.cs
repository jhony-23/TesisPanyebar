using Panyebar.Domain.Enums;

namespace Panyebar.Application.Pagos;

public sealed record ObligacionPagoDto(
    int Id,
    OrigenObligacion Origen,
    string Concepto,
    string? Periodo,
    decimal Monto);
