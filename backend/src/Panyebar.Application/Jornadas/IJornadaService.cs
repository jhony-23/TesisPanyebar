using Panyebar.Domain.Enums;

namespace Panyebar.Application.Jornadas;

public interface IJornadaService
{
    Task<IReadOnlyList<JornadaResumenDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<JornadaDetalleDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<JornadaOperationResult<JornadaDetalleDto>> CreateAsync(
        JornadaInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);

    Task<JornadaOperationResult<JornadaDetalleDto>> UpdateAsync(
        int id,
        JornadaInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);

    Task<JornadaOperationResult<JornadaDetalleDto>> CancelAsync(
        int id,
        CancelarJornadaInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);

    Task<JornadaOperationResult<JornadaDetalleDto>> AddParticipantsAsync(
        int id,
        AgregarParticipantesJornadaInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);

    Task<JornadaOperationResult<JornadaDetalleDto>> RemoveParticipantAsync(
        int id,
        int personaId,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);

    Task<JornadaOperationResult<JornadaDetalleDto>> UpdateParticipantAsync(
        int id,
        int personaId,
        ActualizarParticipacionJornadaInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);

    Task<JornadaOperationResult<JornadaDetalleDto>> CloseAsync(
        int id,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default);
}

public sealed record JornadaInput(
    string? Nombre,
    string? Descripcion,
    DateTime Fecha,
    TimeOnly? HoraInicio,
    TimeOnly? HoraFin,
    string? Ubicacion,
    decimal? MontoIncumplimiento);

public sealed record CancelarJornadaInput(string? Motivo);

public sealed record AgregarParticipantesJornadaInput(
    IReadOnlyCollection<int>? PersonaIds);

public sealed record ActualizarParticipacionJornadaInput(
    ResultadoParticipacionJornada Resultado,
    string? Observacion);

public sealed record JornadaResumenDto(
    int Id,
    string Nombre,
    string? Descripcion,
    DateTime Fecha,
    TimeOnly? HoraInicio,
    TimeOnly? HoraFin,
    string? Ubicacion,
    decimal? MontoIncumplimiento,
    EstadoJornada Estado,
    int CantidadParticipantes);

public sealed record JornadaDetalleDto(
    int Id,
    string Nombre,
    string? Descripcion,
    DateTime Fecha,
    TimeOnly? HoraInicio,
    TimeOnly? HoraFin,
    string? Ubicacion,
    decimal? MontoIncumplimiento,
    EstadoJornada Estado,
    IReadOnlyList<ParticipacionJornadaDto> Participantes);

public sealed record ParticipacionJornadaDto(
    int Id,
    int PersonaId,
    string NombrePersona,
    ResultadoParticipacionJornada Resultado,
    string? Observacion,
    int? ObligacionId,
    EstadoObligacion? EstadoObligacion);

public enum JornadaOperationError
{
    None,
    Invalid,
    NotFound,
    Conflict
}

public sealed record JornadaOperationResult<T>(
    T? Value,
    JornadaOperationError Error)
{
    public bool Succeeded => Error == JornadaOperationError.None;

    public static JornadaOperationResult<T> Success(T value) =>
        new(value, JornadaOperationError.None);

    public static JornadaOperationResult<T> Failure(JornadaOperationError error) =>
        new(default, error);
}
