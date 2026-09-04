using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Personas;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;

namespace Panyebar.Infrastructure.Persistence;

public sealed class PersonaService : IPersonaService
{
    private readonly PanyebarDbContext _dbContext;

    public PersonaService(PanyebarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PersonaDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Personas
            .AsNoTracking()
            .OrderBy(p => p.Apellidos)
            .ThenBy(p => p.Nombres)
            .ThenBy(p => p.Id)
            .Select(p => new PersonaDto(
                p.Id,
                p.Nombres,
                p.Apellidos,
                p.Identificacion,
                p.Telefono,
                p.DireccionReferencia,
                p.Estado))
            .ToListAsync(cancellationToken);
    }

    public async Task<PersonaDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Personas
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PersonaDto(
                p.Id,
                p.Nombres,
                p.Apellidos,
                p.Identificacion,
                p.Telefono,
                p.DireccionReferencia,
                p.Estado))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PersonaOperationResult<PersonaDto>> CreateAsync(
        PersonaInput input,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(input);
        if (normalized is null)
        {
            return PersonaOperationResult<PersonaDto>.Failure(PersonaOperationError.Invalid);
        }

        if (await IdentificationExistsAsync(normalized.Value.Identificacion, null, cancellationToken))
        {
            return PersonaOperationResult<PersonaDto>.Failure(PersonaOperationError.Duplicate);
        }

        var persona = new Persona
        {
            Nombres = normalized.Value.Nombres,
            Apellidos = normalized.Value.Apellidos,
            Identificacion = normalized.Value.Identificacion,
            Telefono = normalized.Value.Telefono,
            DireccionReferencia = normalized.Value.DireccionReferencia,
            Estado = EstadoRegistro.Activo
        };

        _dbContext.Personas.Add(persona);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _dbContext.Entry(persona).State = EntityState.Detached;
            return PersonaOperationResult<PersonaDto>.Failure(PersonaOperationError.Duplicate);
        }

        return PersonaOperationResult<PersonaDto>.Success(ToDto(persona));
    }

    public async Task<PersonaOperationResult<PersonaDto>> UpdateAsync(
        int id,
        PersonaInput input,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(input);
        if (normalized is null)
        {
            return PersonaOperationResult<PersonaDto>.Failure(PersonaOperationError.Invalid);
        }

        var persona = await _dbContext.Personas
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (persona is null)
        {
            return PersonaOperationResult<PersonaDto>.Failure(PersonaOperationError.NotFound);
        }

        if (await IdentificationExistsAsync(normalized.Value.Identificacion, id, cancellationToken))
        {
            return PersonaOperationResult<PersonaDto>.Failure(PersonaOperationError.Duplicate);
        }

        persona.Nombres = normalized.Value.Nombres;
        persona.Apellidos = normalized.Value.Apellidos;
        persona.Identificacion = normalized.Value.Identificacion;
        persona.Telefono = normalized.Value.Telefono;
        persona.DireccionReferencia = normalized.Value.DireccionReferencia;
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _dbContext.Entry(persona).State = EntityState.Detached;
            return PersonaOperationResult<PersonaDto>.Failure(PersonaOperationError.Duplicate);
        }

        return PersonaOperationResult<PersonaDto>.Success(ToDto(persona));
    }

    public async Task<PersonaOperationResult<PersonaDto>> SetEstadoAsync(
        int id,
        EstadoRegistro estado,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(typeof(EstadoRegistro), estado))
        {
            return PersonaOperationResult<PersonaDto>.Failure(PersonaOperationError.Invalid);
        }

        var persona = await _dbContext.Personas
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (persona is null)
        {
            return PersonaOperationResult<PersonaDto>.Failure(PersonaOperationError.NotFound);
        }

        persona.Estado = estado;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return PersonaOperationResult<PersonaDto>.Success(ToDto(persona));
    }

    private async Task<bool> IdentificationExistsAsync(
        string? identificacion,
        int? excludedId,
        CancellationToken cancellationToken)
    {
        if (identificacion is null)
        {
            return false;
        }

        return await _dbContext.Personas
            .AsNoTracking()
            .Where(p => excludedId == null || p.Id != excludedId.Value)
            .AnyAsync(p => EF.Functions.Collate(p.Identificacion!, "Latin1_General_100_CI_AS") == identificacion, cancellationToken);
    }

    private static (string Nombres, string Apellidos, string? Identificacion, string? Telefono, string? DireccionReferencia)? Normalize(PersonaInput input)
    {
        if (input is null || string.IsNullOrWhiteSpace(input.Nombres) || input.Nombres.Trim().Length > 150 ||
            string.IsNullOrWhiteSpace(input.Apellidos) || input.Apellidos.Trim().Length > 150)
        {
            return null;
        }

        var identificacion = NormalizeOptional(input.Identificacion);
        var telefono = NormalizeOptional(input.Telefono);
        var direccionReferencia = NormalizeOptional(input.DireccionReferencia);

        if (identificacion?.Length > 50 || telefono?.Length > 30 || direccionReferencia?.Length > 500)
        {
            return null;
        }

        return (input.Nombres.Trim(), input.Apellidos.Trim(), identificacion, telefono, direccionReferencia);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static PersonaDto ToDto(Persona persona) =>
        new(persona.Id, persona.Nombres, persona.Apellidos, persona.Identificacion, persona.Telefono, persona.DireccionReferencia, persona.Estado);

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (var current = exception.InnerException; current is not null; current = current.InnerException)
        {
            if (current is SqlException sqlException &&
                sqlException.Errors.Cast<SqlError>().Any(error => error.Number is 2601 or 2627))
            {
                return true;
            }
        }

        return false;
    }
}
