using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Cuotas;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;

namespace Panyebar.Infrastructure.Persistence;

public sealed class CuotaService : ICuotaService
{
    private const decimal MaximumAmount = 9999999999999999.99m;
    private readonly PanyebarDbContext _dbContext;

    public CuotaService(PanyebarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CuotaDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Cuotas
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .ThenBy(c => c.Id)
            .Select(c => new CuotaDto(
                c.Id,
                c.Nombre,
                c.Descripcion,
                c.Monto,
                c.Periodicidad,
                c.FechaInicioVigencia,
                c.FechaFinVigencia,
                c.Estado))
            .ToListAsync(cancellationToken);
    }

    public Task<CuotaDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Cuotas
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CuotaDto(
                c.Id,
                c.Nombre,
                c.Descripcion,
                c.Monto,
                c.Periodicidad,
                c.FechaInicioVigencia,
                c.FechaFinVigencia,
                c.Estado))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CuotaOperationResult<CuotaDto>> CreateAsync(
        CuotaInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(input);
        if (normalized is null || !await UserExistsAsync(usuarioAdministrativoId, cancellationToken))
        {
            return Failure(CuotaOperationError.Invalid);
        }

        var cuota = new Cuota
        {
            Nombre = normalized.Value.Nombre,
            Descripcion = normalized.Value.Descripcion,
            Monto = normalized.Value.Monto,
            Periodicidad = normalized.Value.Periodicidad,
            FechaInicioVigencia = normalized.Value.FechaInicioVigencia,
            FechaFinVigencia = normalized.Value.FechaFinVigencia,
            Estado = EstadoRegistro.Activo
        };

        await using var transaction = await BeginTransactionIfRelationalAsync(cancellationToken);
        _dbContext.Cuotas.Add(cuota);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.Auditorias.Add(CreateAudit(
            usuarioAdministrativoId,
            "CUOTA.CREAR",
            cuota.Id,
            null,
            Describe(cuota),
            DateTime.UtcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return CuotaOperationResult<CuotaDto>.Success(ToDto(cuota));
    }

    public async Task<CuotaOperationResult<CuotaDto>> UpdateAsync(
        int id,
        CuotaInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(input);
        if (id <= 0 || normalized is null || !await UserExistsAsync(usuarioAdministrativoId, cancellationToken))
        {
            return Failure(CuotaOperationError.Invalid);
        }

        var cuota = await _dbContext.Cuotas.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (cuota is null)
        {
            return Failure(CuotaOperationError.NotFound);
        }

        var previousValue = Describe(cuota);
        cuota.Nombre = normalized.Value.Nombre;
        cuota.Descripcion = normalized.Value.Descripcion;
        cuota.Monto = normalized.Value.Monto;
        cuota.Periodicidad = normalized.Value.Periodicidad;
        cuota.FechaInicioVigencia = normalized.Value.FechaInicioVigencia;
        cuota.FechaFinVigencia = normalized.Value.FechaFinVigencia;

        _dbContext.Auditorias.Add(CreateAudit(
            usuarioAdministrativoId,
            "CUOTA.ACTUALIZAR",
            cuota.Id,
            previousValue,
            Describe(cuota),
            DateTime.UtcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CuotaOperationResult<CuotaDto>.Success(ToDto(cuota));
    }

    public async Task<CuotaOperationResult<CuotaDto>> SetEstadoAsync(
        int id,
        EstadoRegistro estado,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0 || !Enum.IsDefined(typeof(EstadoRegistro), estado) ||
            !await UserExistsAsync(usuarioAdministrativoId, cancellationToken))
        {
            return Failure(CuotaOperationError.Invalid);
        }

        var cuota = await _dbContext.Cuotas.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (cuota is null)
        {
            return Failure(CuotaOperationError.NotFound);
        }

        var previousState = cuota.Estado;
        cuota.Estado = estado;
        _dbContext.Auditorias.Add(CreateAudit(
            usuarioAdministrativoId,
            "CUOTA.ESTADO.ACTUALIZAR",
            cuota.Id,
            previousState.ToString(),
            estado.ToString(),
            DateTime.UtcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CuotaOperationResult<CuotaDto>.Success(ToDto(cuota));
    }

    private Task<bool> UserExistsAsync(int userId, CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            return Task.FromResult(false);
        }

        return _dbContext.UsuariosAdministrativos
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId, cancellationToken);
    }

    private static (string Nombre, string? Descripcion, decimal Monto, PeriodicidadCuota Periodicidad, DateTime FechaInicioVigencia, DateTime? FechaFinVigencia)? Normalize(CuotaInput input)
    {
        if (input is null || string.IsNullOrWhiteSpace(input.Nombre) ||
            !Enum.IsDefined(typeof(PeriodicidadCuota), input.Periodicidad) ||
            input.Monto < -MaximumAmount || input.Monto > MaximumAmount || decimal.Round(input.Monto, 2) != input.Monto ||
            input.FechaInicioVigencia == default ||
            input.FechaFinVigencia.HasValue && input.FechaFinVigencia.Value < input.FechaInicioVigencia)
        {
            return null;
        }

        var name = input.Nombre.Trim();
        var description = string.IsNullOrWhiteSpace(input.Descripcion) ? null : input.Descripcion.Trim();
        if (name.Length > 100 || description?.Length > 500)
        {
            return null;
        }

        return (name, description, input.Monto, input.Periodicidad, input.FechaInicioVigencia, input.FechaFinVigencia);
    }

    private static CuotaDto ToDto(Cuota cuota) => new(
        cuota.Id,
        cuota.Nombre,
        cuota.Descripcion,
        cuota.Monto,
        cuota.Periodicidad,
        cuota.FechaInicioVigencia,
        cuota.FechaFinVigencia,
        cuota.Estado);

    private static string Describe(Cuota cuota) =>
        $"Nombre:{cuota.Nombre}; Monto:{cuota.Monto:0.00}; Periodicidad:{cuota.Periodicidad}; " +
        $"Vigencia:{cuota.FechaInicioVigencia:O}-{cuota.FechaFinVigencia:O}; Estado:{cuota.Estado}";

    private static Auditoria CreateAudit(
        int userId,
        string action,
        int entityId,
        string? previousValue,
        string newValue,
        DateTime date) => new()
        {
            UsuarioAdministrativoId = userId,
            Accion = action,
            Entidad = "Cuota",
            EntidadId = entityId,
            Fecha = date,
            ValorAnterior = previousValue,
            ValorNuevo = newValue
        };

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?> BeginTransactionIfRelationalAsync(
        CancellationToken cancellationToken)
    {
        return _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
    }

    private static CuotaOperationResult<CuotaDto> Failure(CuotaOperationError error) =>
        CuotaOperationResult<CuotaDto>.Failure(error);
}
