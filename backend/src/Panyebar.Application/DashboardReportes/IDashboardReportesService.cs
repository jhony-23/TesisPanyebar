using Panyebar.Domain.Enums;

namespace Panyebar.Application.DashboardReportes;

public interface IDashboardReportesService
{
    Task<ConsultaResult<IReadOnlyList<RecaudacionSectorDto>>> GetRecaudacionPorSectorAsync(
        DateOnly? fechaDesde, DateOnly? fechaHasta, CancellationToken cancellationToken = default);
    Task<ConsultaResult<DashboardResumenDto>> GetResumenAsync(
        int anio, int mes, CancellationToken cancellationToken = default);
    Task<ConsultaResult<IReadOnlyList<ReportePagoDto>>> GetPagosAsync(
        DateOnly? fechaDesde, DateOnly? fechaHasta, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReporteObligacionPendienteDto>> GetObligacionesPendientesAsync(
        CancellationToken cancellationToken = default);
    Task<ConsultaResult<IReadOnlyList<ReporteParticipacionJornadaDto>>> GetJornadasAsync(
        DateOnly? fechaDesde, DateOnly? fechaHasta, CancellationToken cancellationToken = default);
}

// Estados actuales, filtrados por el mes administrativo de Guatemala.
// Las obligaciones pertenecen al mes por FechaGeneracion, no por vencimiento ni Periodo.
public sealed record DashboardResumenDto(
    int Anio, int Mes, decimal IngresosTotales, decimal EgresosTotales, decimal Balance,
    int CantidadPagos, int CantidadObligacionesPendientes, decimal MontoObligacionesPendientes);

// El reporte histórico incluye Registrado y Anulado. Solo Registrado representa ingreso.
public sealed record ReportePagoDto(
    int PagoId, DateTime Fecha, string Concepto, decimal Monto, EstadoPago Estado,
    int UsuarioAdministrativoId);

public sealed record RecaudacionSectorDto(int SectorId, string NombreSector, decimal MontoTotal);

// Titularidad XOR: NombrePersona corresponde a PersonaId; Nis corresponde a SuministroId.
public sealed record ReporteObligacionPendienteDto(
    int ObligacionId, int? PersonaId, string? NombrePersona, int? SuministroId, string? Nis,
    OrigenObligacion Origen, string Concepto, decimal Monto, string? Periodo,
    DateTime FechaGeneracion, DateTime? FechaVencimiento, bool EsMorosa);

// FechaJornada es civil, sin conversión UTC. No existe fecha de asistencia en el modelo.
public sealed record ReporteParticipacionJornadaDto(
    int ParticipacionId, int JornadaId, string NombreJornada, DateTime FechaJornada,
    EstadoJornada EstadoJornada, int PersonaId, string NombrePersona,
    ResultadoParticipacionJornada Resultado, string? Observacion);

public sealed record ConsultaResult<T>(T? Value, bool Succeeded)
{
    public static ConsultaResult<T> Success(T value) => new(value, true);
    public static ConsultaResult<T> Invalid() => new(default, false);
}
