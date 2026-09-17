using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Pagos;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;

namespace Panyebar.Infrastructure.Persistence;

public sealed class PagoService : IPagoService
{
    private const string TipoPersona = "Persona";
    private const string TipoSuministro = "Suministro";

    private readonly PanyebarDbContext _dbContext;

    public PagoService(PanyebarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PagoDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var pagos = await _dbContext.Pagos
            .AsNoTracking()
            .OrderByDescending(p => p.Fecha)
            .ThenByDescending(p => p.Id)
            .ToListAsync(cancellationToken);

        var result = new List<PagoDto>(pagos.Count);

        foreach (var pago in pagos)
        {
            var detail = await GetByIdAsync(pago.Id, cancellationToken);

            if (detail is not null)
            {
                result.Add(new PagoDto(
                    detail.Id,
                    detail.NumeroComprobante,
                    detail.Monto,
                    detail.Fecha,
                    detail.Concepto,
                    detail.Estado,
                    detail.UsuarioAdministrativoId,
                    detail.Titular));
            }
        }

        return result;
    }

    public async Task<PagoDetalleDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var pago = await _dbContext.Pagos
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (pago is null)
        {
            return null;
        }

        var usuario = await _dbContext.UsuariosAdministrativos
            .AsNoTracking()
            .SingleOrDefaultAsync(
                u => u.Id == pago.UsuarioAdministrativoId,
                cancellationToken);

        var obligaciones = await (
            from aplicacion in _dbContext.AplicacionesPago.AsNoTracking()
            join obligacion in _dbContext.Obligaciones.AsNoTracking()
                on aplicacion.ObligacionId equals obligacion.Id
            where aplicacion.PagoId == pago.Id
            orderby obligacion.Id
            select obligacion)
            .ToListAsync(cancellationToken);

        if (obligaciones.Count == 0)
        {
            return null;
        }

        var titular = await ResolveTitularAsync(
            obligaciones,
            cancellationToken);

        if (titular is null)
        {
            return null;
        }

        return new PagoDetalleDto(
            pago.Id,
            NumeroComprobante(pago.Id),
            pago.Monto,
            pago.Fecha,
            pago.Concepto,
            pago.Estado,
            pago.UsuarioAdministrativoId,
            usuario?.NombreUsuario ?? $"Usuario #{pago.UsuarioAdministrativoId}",
            titular,
            obligaciones.Select(ToObligacionPagoDto).ToList());
    }

    public async Task<PagoOperationResult<PagoDetalleDto>> RegisterAsync(
        RegistrarPagoInput input,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidInput(input))
        {
            return Failure(PagoOperationError.Invalid);
        }

        var usuarioExiste = await _dbContext.UsuariosAdministrativos
            .AsNoTracking()
            .AnyAsync(
                u => u.Id == usuarioAdministrativoId,
                cancellationToken);

        if (!usuarioExiste)
        {
            return Failure(PagoOperationError.Invalid);
        }

        var obligationIds = input.ObligacionIds.ToArray();

        var obligaciones = await _dbContext.Obligaciones
            .Where(o => obligationIds.Contains(o.Id))
            .OrderBy(o => o.Id)
            .ToListAsync(cancellationToken);

        if (obligaciones.Count != obligationIds.Length)
        {
            return Failure(PagoOperationError.NotFound);
        }

        if (obligaciones.Any(o => o.Estado != EstadoObligacion.Pendiente))
        {
            return Failure(PagoOperationError.Conflict);
        }

        if (!HaveSameOwner(obligaciones))
        {
            return Failure(PagoOperationError.Conflict);
        }

        var total = obligaciones.Sum(o => o.Monto);

        if (total != input.Monto)
        {
            return Failure(PagoOperationError.Invalid);
        }

        var titular = await ResolveTitularAsync(
            obligaciones,
            cancellationToken);

        if (titular is null)
        {
            return Failure(PagoOperationError.NotFound);
        }

        await using var transaction =
            await BeginTransactionIfRelationalAsync(cancellationToken);

        var now = DateTime.UtcNow;

        var pago = new Pago
        {
            Monto = input.Monto,
            Fecha = now,
            Concepto = input.Concepto.Trim(),
            UsuarioAdministrativoId = usuarioAdministrativoId,
            Estado = EstadoPago.Registrado
        };

        _dbContext.Pagos.Add(pago);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);

            foreach (var obligacion in obligaciones)
            {
                _dbContext.AplicacionesPago.Add(new AplicacionPago
                {
                    PagoId = pago.Id,
                    ObligacionId = obligacion.Id
                });

                obligacion.Estado = EstadoObligacion.Pagada;
            }

            _dbContext.Auditorias.Add(new Auditoria
            {
                UsuarioAdministrativoId = usuarioAdministrativoId,
                Accion = "PAGO.REGISTRAR",
                Entidad = nameof(Pago),
                EntidadId = pago.Id,
                Fecha = now,
                ValorAnterior = null,
                ValorNuevo =
                    $"Monto:{pago.Monto:0.00}; " +
                    $"ObligacionIds:{string.Join(",", obligaciones.Select(o => o.Id))}; " +
                    $"Estado:{pago.Estado}"
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateException exception)
            when (IsUniqueConstraintViolation(exception))
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            _dbContext.ChangeTracker.Clear();
            return Failure(PagoOperationError.Conflict);
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }

        var detail = await GetByIdAsync(pago.Id, cancellationToken);

        return detail is null
            ? Failure(PagoOperationError.NotFound)
            : PagoOperationResult<PagoDetalleDto>.Success(detail);
    }

    public async Task<PagoOperationResult<PagoDetalleDto>> AnnulAsync(
        int id,
        int usuarioAdministrativoId,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0 || usuarioAdministrativoId <= 0)
        {
            return Failure(PagoOperationError.Invalid);
        }

        var usuarioExiste = await _dbContext.UsuariosAdministrativos
            .AsNoTracking()
            .AnyAsync(
                u => u.Id == usuarioAdministrativoId,
                cancellationToken);

        if (!usuarioExiste)
        {
            return Failure(PagoOperationError.Invalid);
        }

        var pago = await _dbContext.Pagos
            .SingleOrDefaultAsync(
                p => p.Id == id,
                cancellationToken);

        if (pago is null)
        {
            return Failure(PagoOperationError.NotFound);
        }

        if (pago.Estado != EstadoPago.Registrado)
        {
            return Failure(PagoOperationError.Conflict);
        }

        var aplicaciones = await _dbContext.AplicacionesPago
            .Where(ap => ap.PagoId == pago.Id)
            .OrderBy(ap => ap.Id)
            .ToListAsync(cancellationToken);

        if (aplicaciones.Count == 0)
        {
            return Failure(PagoOperationError.Conflict);
        }

        var obligacionIds = aplicaciones
            .Select(ap => ap.ObligacionId)
            .Distinct()
            .ToArray();

        var obligaciones = await _dbContext.Obligaciones
            .Where(o => obligacionIds.Contains(o.Id))
            .OrderBy(o => o.Id)
            .ToListAsync(cancellationToken);

        if (obligaciones.Count != obligacionIds.Length)
        {
            return Failure(PagoOperationError.Conflict);
        }

        if (obligaciones.Any(o => o.Estado != EstadoObligacion.Pagada))
        {
            return Failure(PagoOperationError.Conflict);
        }

        await using var transaction =
            await BeginTransactionIfRelationalAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var valorAnterior =
            $"Monto:{pago.Monto:0.00}; " +
            $"ObligacionIds:{string.Join(",", obligacionIds)}; " +
            $"Estado:{pago.Estado}";

        pago.Estado = EstadoPago.Anulado;

        foreach (var obligacion in obligaciones)
        {
            obligacion.Estado = EstadoObligacion.Pendiente;
        }

        _dbContext.Auditorias.Add(new Auditoria
        {
            UsuarioAdministrativoId = usuarioAdministrativoId,
            Accion = "PAGO.ANULAR",
            Entidad = nameof(Pago),
            EntidadId = pago.Id,
            Fecha = now,
            ValorAnterior = valorAnterior,
            ValorNuevo =
                $"Monto:{pago.Monto:0.00}; " +
                $"ObligacionIds:{string.Join(",", obligacionIds)}; " +
                $"Estado:{pago.Estado}"
        });

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }

        var detail = await GetByIdAsync(pago.Id, cancellationToken);

        return detail is null
            ? Failure(PagoOperationError.NotFound)
            : PagoOperationResult<PagoDetalleDto>.Success(detail);
    }
    public async Task<ComprobantePagoDto?> GetComprobanteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var pago = await GetByIdAsync(id, cancellationToken);

        if (pago is null)
        {
            return null;
        }

        return new ComprobantePagoDto(
            pago.NumeroComprobante,
            pago.Id,
            pago.Fecha,
            pago.Concepto,
            pago.Monto,
            pago.Estado.ToString(),
            pago.UsuarioAdministrativo,
            pago.Titular,
            pago.Obligaciones);
    }

    private static bool IsValidInput(RegistrarPagoInput input)
    {
        if (input.Monto <= 0 ||
            string.IsNullOrWhiteSpace(input.Concepto) ||
            input.Concepto.Trim().Length > 200 ||
            input.ObligacionIds is null ||
            input.ObligacionIds.Count == 0)
        {
            return false;
        }

        var ids = input.ObligacionIds.ToArray();

        return ids.All(id => id > 0) &&
               ids.Distinct().Count() == ids.Length;
    }

    private static bool HaveSameOwner(IReadOnlyCollection<Obligacion> obligaciones)
    {
        if (obligaciones.Count == 0)
        {
            return false;
        }

        var first = obligaciones.First();

        if (first.SuministroId.HasValue)
        {
            return !first.PersonaId.HasValue &&
                   obligaciones.All(o =>
                       o.SuministroId == first.SuministroId &&
                       !o.PersonaId.HasValue);
        }

        if (first.PersonaId.HasValue)
        {
            return !first.SuministroId.HasValue &&
                   obligaciones.All(o =>
                       o.PersonaId == first.PersonaId &&
                       !o.SuministroId.HasValue);
        }

        return false;
    }

    private async Task<TitularPagoDto?> ResolveTitularAsync(
        IReadOnlyCollection<Obligacion> obligaciones,
        CancellationToken cancellationToken)
    {
        if (obligaciones.Count == 0)
        {
            return null;
        }

        var first = obligaciones.First();

        if (first.SuministroId is int suministroId)
        {
            var suministro = await _dbContext.Suministros
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    s => s.Id == suministroId,
                    cancellationToken);

            if (suministro is null)
            {
                return null;
            }

            var relacion = await _dbContext.PersonaSuministros
                .AsNoTracking()
                .Where(ps =>
                    ps.SuministroId == suministroId &&
                    ps.Estado == EstadoRelacionSuministro.Vigente)
                .SingleOrDefaultAsync(cancellationToken);

            string nombre;

            if (relacion is null)
            {
                nombre = $"Suministro {suministro.Nis}";
            }
            else
            {
                var persona = await _dbContext.Personas
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        p => p.Id == relacion.PersonaId,
                        cancellationToken);

                nombre = persona is null
                    ? $"Suministro {suministro.Nis}"
                    : NombrePersona(persona);
            }

            return new TitularPagoDto(
                TipoSuministro,
                suministro.Id,
                nombre,
                suministro.Nis);
        }

        if (first.PersonaId is int personaId)
        {
            var persona = await _dbContext.Personas
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    p => p.Id == personaId,
                    cancellationToken);

            return persona is null
                ? null
                : new TitularPagoDto(
                    TipoPersona,
                    persona.Id,
                    NombrePersona(persona),
                    null);
        }

        return null;
    }

    private static string NombrePersona(Persona persona) =>
        $"{persona.Nombres} {persona.Apellidos}".Trim();

    private static ObligacionPagoDto ToObligacionPagoDto(
        Obligacion obligacion) =>
        new(
            obligacion.Id,
            obligacion.Origen,
            obligacion.Concepto,
            obligacion.Periodo,
            obligacion.Monto);

    private static string NumeroComprobante(int pagoId) =>
        $"PAG-{pagoId:D6}";

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?>
        BeginTransactionIfRelationalAsync(
            CancellationToken cancellationToken)
    {
        return _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
    }

    private static bool IsUniqueConstraintViolation(
        DbUpdateException exception)
    {
        for (var current = exception.InnerException;
             current is not null;
             current = current.InnerException)
        {
            if (current is SqlException sqlException &&
                (sqlException.Number == 2601 ||
                 sqlException.Number == 2627))
            {
                return true;
            }
        }

        return false;
    }

    private static PagoOperationResult<PagoDetalleDto> Failure(
        PagoOperationError error) =>
        PagoOperationResult<PagoDetalleDto>.Failure(error);
}
