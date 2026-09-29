using Panyebar.Domain.Enums;

namespace Panyebar.Application.Personas;

public interface IPersonaService
{
    Task<IReadOnlyList<PersonaDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<PersonaDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PersonaOperationResult<PersonaDto>> CreateAsync(PersonaInput input, CancellationToken cancellationToken = default);
    Task<PersonaOperationResult<PersonaDto>> UpdateAsync(int id, PersonaInput input, CancellationToken cancellationToken = default);
    Task<PersonaOperationResult<PersonaDto>> SetEstadoAsync(int id, EstadoRegistro estado, CancellationToken cancellationToken = default);
}

public sealed record PersonaDto(
    int Id,
    string Nombres,
    string Apellidos,
    string? Identificacion,
    string? Telefono,
    string? DireccionReferencia,
    EstadoRegistro Estado,
    int? SectorId = null,
    string? SectorNombre = null);

public sealed record PersonaInput(
    string? Nombres,
    string? Apellidos,
    string? Identificacion,
    string? Telefono,
    string? DireccionReferencia,
    int? SectorId = null);

public enum PersonaOperationError
{
    None,
    Invalid,
    NotFound,
    Duplicate
}

public sealed record PersonaOperationResult<T>(T? Value, PersonaOperationError Error)
{
    public static PersonaOperationResult<T> Success(T value) => new(value, PersonaOperationError.None);
    public static PersonaOperationResult<T> Failure(PersonaOperationError error) => new(default, error);
}
