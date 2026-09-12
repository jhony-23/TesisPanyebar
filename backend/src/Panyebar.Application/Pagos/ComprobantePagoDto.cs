namespace Panyebar.Application.Pagos;

public sealed record ComprobantePagoDto(
    string Numero,
    int PagoId,
    DateTime Fecha,
    string Concepto,
    decimal Total,
    string Estado,
    string UsuarioAdministrativo,
    TitularPagoDto Titular,
    IReadOnlyList<ObligacionPagoDto> Obligaciones);
