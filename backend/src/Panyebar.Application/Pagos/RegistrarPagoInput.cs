namespace Panyebar.Application.Pagos;

public sealed record RegistrarPagoInput(
    decimal Monto,
    string Concepto,
    IReadOnlyCollection<int> ObligacionIds);
