using Panyebar.Domain.Enums;

namespace Panyebar.Application.Finanzas;

public interface IFinanzaService
{
    Task<FinanzaOperationResult<IReadOnlyList<EgresoDto>>> GetEgresosAsync(DateOnly? fechaDesde, DateOnly? fechaHasta, EstadoEgreso? estado, CancellationToken cancellationToken = default);
    Task<EgresoDto?> GetEgresoByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<FinanzaOperationResult<EgresoDto>> RegisterAsync(EgresoInput input, int usuarioAdministrativoId, CancellationToken cancellationToken = default);
    Task<FinanzaOperationResult<EgresoDto>> UpdateAsync(int id, EgresoInput input, int usuarioAdministrativoId, CancellationToken cancellationToken = default);
    Task<FinanzaOperationResult<EgresoDto>> AnnulAsync(int id, int usuarioAdministrativoId, CancellationToken cancellationToken = default);
    Task<FinanzaOperationResult<IReadOnlyList<IngresoFinancieroDto>>> GetIngresosAsync(DateOnly? fechaDesde, DateOnly? fechaHasta, CancellationToken cancellationToken = default);
    Task<FinanzaOperationResult<IReadOnlyList<MovimientoFinancieroDto>>> GetMovimientosAsync(DateOnly? fechaDesde, DateOnly? fechaHasta, TipoMovimientoFinanciero? tipo, CancellationToken cancellationToken = default);
    Task<FinanzaOperationResult<ResumenFinancieroDto>> GetResumenAsync(DateOnly? fechaDesde, DateOnly? fechaHasta, CancellationToken cancellationToken = default);
}

// Fechas civiles se intercambian como YYYY-MM-DD, sin zona ni hora.
public sealed record EgresoInput(string? Concepto, decimal Monto, DateOnly Fecha);
public sealed record EgresoDto(int Id, string Concepto, decimal Monto, DateOnly Fecha, int UsuarioAdministrativoId, EstadoEgreso Estado);

// Fecha conserva el instante UTC certificado del pago; FechaOperativa es su día en Guatemala.
public sealed record IngresoFinancieroDto(int PagoId, DateTime Fecha, DateOnly FechaOperativa, string Concepto, decimal Monto, EstadoPago Estado, int UsuarioAdministrativoId);
public enum TipoMovimientoFinanciero { Ingreso = 1, Egreso = 2 }

// Fecha es civil para ambas fuentes. El par Tipo/ReferenciaId identifica el movimiento.
public sealed record MovimientoFinancieroDto(TipoMovimientoFinanciero Tipo, int ReferenciaId, DateOnly Fecha, string Concepto, decimal Monto, string Estado);
public sealed record ResumenFinancieroDto(decimal IngresosTotales, decimal EgresosTotales, decimal Balance);
public enum FinanzaOperationError { None, Invalid, NotFound, Conflict }
public sealed record FinanzaOperationResult<T>(T? Value, FinanzaOperationError Error)
{
    public bool Succeeded => Error == FinanzaOperationError.None;
    public static FinanzaOperationResult<T> Success(T value) => new(value, FinanzaOperationError.None);
    public static FinanzaOperationResult<T> Failure(FinanzaOperationError error) => new(default, error);
}
