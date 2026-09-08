using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Suministros;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;
using Panyebar.Infrastructure.Security;

namespace Panyebar.Security.Tests;

public class SuministroServiceTests
{
    [Fact]
    public async Task CreateAsync_GeneratesAndKeepsNisAndQrToken()
    {
        await using var dbContext = CreateContext();
        dbContext.Sectores.Add(new Sector { Id = 1, Nombre = "Centro", Estado = EstadoRegistro.Activo });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, "PAN-000321", "token-creado");

        var result = await service.CreateAsync(new SuministroInput(1, " Calle Principal "));

        Assert.True(result.Succeeded);
        Assert.Equal("PAN-000321", result.Value!.Nis);
        Assert.Equal("token-creado", await dbContext.Suministros.Select(s => s.CodigoQrToken).SingleAsync());
        Assert.Equal("Calle Principal", result.Value.DireccionReferencia);
        Assert.Equal(EstadoSuministro.Activo, result.Value.Estado);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotAlterNisOrQrToken()
    {
        await using var dbContext = CreateContext();
        dbContext.Sectores.AddRange(
            new Sector { Id = 1, Nombre = "Centro", Estado = EstadoRegistro.Activo },
            new Sector { Id = 2, Nombre = "Norte", Estado = EstadoRegistro.Activo });
        dbContext.Suministros.Add(new Suministro
        {
            Id = 10,
            SectorId = 1,
            Nis = "PAN-000010",
            CodigoQrToken = "token-original",
            DireccionReferencia = "Anterior"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, "PAN-999999", "token-nuevo-no-usado");

        var result = await service.UpdateAsync(10, new SuministroInput(2, "Nueva dirección"));

        Assert.True(result.Succeeded);
        Assert.Equal("PAN-000010", result.Value!.Nis);
        Assert.Equal("token-original", await dbContext.Suministros.Select(s => s.CodigoQrToken).SingleAsync());
        Assert.Equal(2, result.Value.SectorId);
    }

    [Fact]
    public async Task CreateAsync_RejectsMissingOrInactiveSector()
    {
        await using var dbContext = CreateContext();
        dbContext.Sectores.Add(new Sector { Id = 1, Nombre = "Inactivo", Estado = EstadoRegistro.Inactivo });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, "PAN-000001", "token");

        var missing = await service.CreateAsync(new SuministroInput(99, "Dirección"));
        var inactive = await service.CreateAsync(new SuministroInput(1, "Dirección"));

        Assert.Equal(SuministroOperationError.Invalid, missing.Error);
        Assert.Equal(SuministroOperationError.Invalid, inactive.Error);
        Assert.Empty(await dbContext.Suministros.ToListAsync());
    }

    [Fact]
    public async Task GetByNisAsync_TrimsNisAndReturnsCurrentResponsible()
    {
        await using var dbContext = CreateContext();
        dbContext.Sectores.Add(new Sector { Id = 1, Nombre = "Centro", Estado = EstadoRegistro.Activo });
        dbContext.Personas.Add(new Persona { Id = 4, Nombres = "Ana", Apellidos = "Pérez" });
        dbContext.Suministros.Add(new Suministro
        {
            Id = 10,
            SectorId = 1,
            Nis = "PAN-000010",
            CodigoQrToken = "token",
            DireccionReferencia = "Dirección"
        });
        dbContext.PersonaSuministros.Add(new PersonaSuministro
        {
            PersonaId = 4,
            SuministroId = 10,
            FechaInicio = DateTime.UtcNow,
            Estado = EstadoRelacionSuministro.Vigente
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, "PAN-000011", "token-2");

        var result = await service.GetByNisAsync(" PAN-000010 ");

        Assert.NotNull(result);
        Assert.Equal(10, result!.Id);
        Assert.Equal(4, result.ResponsableActual!.PersonaId);
        Assert.Equal("Ana", result.ResponsableActual.Nombres);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByNisThenIdAndAllowsMissingResponsible()
    {
        await using var dbContext = CreateContext();
        dbContext.Sectores.Add(new Sector { Id = 1, Nombre = "Centro", Estado = EstadoRegistro.Activo });
        dbContext.Suministros.AddRange(
            new Suministro { Id = 2, SectorId = 1, Nis = "PAN-000002", CodigoQrToken = "token-2", DireccionReferencia = "Dos" },
            new Suministro { Id = 1, SectorId = 1, Nis = "PAN-000001", CodigoQrToken = "token-1", DireccionReferencia = "Uno" });
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext, "PAN-000003", "token-3").GetAllAsync();

        Assert.Collection(
            result,
            first =>
            {
                Assert.Equal("PAN-000001", first.Nis);
                Assert.Null(first.ResponsableActual);
            },
            second => Assert.Equal("PAN-000002", second.Nis));
    }

    [Fact]
    public async Task GetResponsablesAsync_ReturnsCurrentFirstAndHandlesMissingSupply()
    {
        await using var dbContext = CreateContext();
        dbContext.Personas.AddRange(
            new Persona { Id = 4, Nombres = "Ana", Apellidos = "Pérez" },
            new Persona { Id = 5, Nombres = "Luis", Apellidos = "Gómez" });
        dbContext.Sectores.Add(new Sector { Id = 1, Nombre = "Centro", Estado = EstadoRegistro.Activo });
        dbContext.Suministros.Add(new Suministro
        {
            Id = 10,
            SectorId = 1,
            Nis = "PAN-000010",
            CodigoQrToken = "token",
            DireccionReferencia = "Dirección"
        });
        dbContext.PersonaSuministros.AddRange(
            new PersonaSuministro
            {
                Id = 1,
                PersonaId = 4,
                SuministroId = 10,
                FechaInicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                FechaFin = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                Estado = EstadoRelacionSuministro.Finalizada
            },
            new PersonaSuministro
            {
                Id = 2,
                PersonaId = 5,
                SuministroId = 10,
                FechaInicio = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                Estado = EstadoRelacionSuministro.Vigente
            });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, "PAN-000011", "token-2");

        var history = await service.GetResponsablesAsync(10);
        var missing = await service.GetResponsablesAsync(99);

        Assert.NotNull(history);
        Assert.Equal(2, history!.Count);
        Assert.Equal(EstadoRelacionSuministro.Vigente, history[0].Estado);
        Assert.Equal("Luis", history[0].Nombres);
        Assert.Equal(EstadoRelacionSuministro.Finalizada, history[1].Estado);
        Assert.Null(missing);
    }

    [Fact]
    public async Task SetResponsableAsync_RejectsInvalidSupplyOrInactivePerson()
    {
        await using var dbContext = CreateContext();
        dbContext.Sectores.Add(new Sector { Id = 1, Nombre = "Centro", Estado = EstadoRegistro.Activo });
        dbContext.Suministros.Add(new Suministro
        {
            Id = 1,
            SectorId = 1,
            Nis = "PAN-000001",
            CodigoQrToken = "token",
            DireccionReferencia = "Dirección"
        });
        dbContext.Personas.Add(new Persona { Id = 4, Nombres = "Ana", Apellidos = "Pérez", Estado = EstadoRegistro.Inactivo });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, "PAN-000001", "token");

        var missingSupply = await service.SetResponsableAsync(99, new SetResponsableInput(4));
        var inactivePerson = await service.SetResponsableAsync(1, new SetResponsableInput(4));

        Assert.Equal(SuministroOperationError.NotFound, missingSupply.Error);
        Assert.Equal(SuministroOperationError.Invalid, inactivePerson.Error);
    }

    [Fact]
    public async Task GetQrAsync_ReturnsRelativeValueWithoutExposingTokenSeparately()
    {
        await using var dbContext = CreateContext();
        dbContext.Suministros.Add(new Suministro
        {
            Id = 10,
            SectorId = 1,
            Nis = "PAN-000010",
            CodigoQrToken = "token-qr-original",
            DireccionReferencia = "Dirección"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, "PAN-000011", "token-2");
        var result = await service.GetQrAsync(10);

        Assert.NotNull(result);
        Assert.Equal(10, result!.SuministroId);
        Assert.Equal("PAN-000010", result.Nis);
        Assert.Equal("/api/suministros/qr/token-qr-original", result.QrValue);
        Assert.DoesNotContain(result.Nis, result.QrValue);
        Assert.Equal("token-qr-original", await dbContext.Suministros.Select(s => s.CodigoQrToken).SingleAsync());
    }

    [Fact]
    public async Task GetByQrTokenAsync_ReturnsSameSupplyAndNullForUnknownToken()
    {
        await using var dbContext = CreateContext();
        dbContext.Sectores.Add(new Sector { Id = 1, Nombre = "Centro", Estado = EstadoRegistro.Activo });
        dbContext.Suministros.Add(new Suministro
        {
            Id = 10,
            SectorId = 1,
            Nis = "PAN-000010",
            CodigoQrToken = "token-qr-original",
            DireccionReferencia = "Dirección"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, "PAN-000011", "token-2");
        var result = await service.GetByQrTokenAsync(" token-qr-original ");
        var missing = await service.GetByQrTokenAsync("missing-token");

        Assert.NotNull(result);
        Assert.Equal(10, result!.Id);
        Assert.Equal("PAN-000010", result.Nis);
        Assert.Null(missing);
    }

    [Fact]
    public async Task CancelAsync_ChangesOnlyStateAndCreatesProcessAndAudit()
    {
        await using var dbContext = CreateContext();
        dbContext.Sectores.Add(new Sector { Id = 1, Nombre = "Centro", Estado = EstadoRegistro.Activo });
        dbContext.Personas.Add(new Persona { Id = 4, Nombres = "Ana", Apellidos = "Pérez" });
        dbContext.UsuariosAdministrativos.Add(new UsuarioAdministrativo { Id = 8, NombreUsuario = "admin" });
        dbContext.Suministros.Add(new Suministro
        {
            Id = 10,
            SectorId = 1,
            Nis = "PAN-000010",
            CodigoQrToken = "token-original",
            DireccionReferencia = "Dirección"
        });
        dbContext.PersonaSuministros.Add(new PersonaSuministro
        {
            PersonaId = 4,
            SuministroId = 10,
            FechaInicio = DateTime.UtcNow,
            Estado = EstadoRelacionSuministro.Vigente
        });
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext, "PAN-000011", "token-2")
            .CancelAsync(10, new SuministroProcesoInput("  Corte solicitado  ", "  Nota interna  "), 8);

        Assert.True(result.Succeeded);
        Assert.Equal(EstadoSuministro.Cancelado, result.Value!.Estado);
        Assert.Equal("PAN-000010", result.Value.Nis);
        Assert.Equal("token-original", await dbContext.Suministros.Select(s => s.CodigoQrToken).SingleAsync());
        Assert.Equal(4, result.Value.ResponsableActual!.PersonaId);

        var process = await dbContext.ProcesosSuministro.SingleAsync();
        Assert.Equal(TipoProcesoSuministro.Cancelacion, process.TipoProceso);
        Assert.Equal(EstadoSuministro.Activo, process.EstadoAnterior);
        Assert.Equal(EstadoSuministro.Cancelado, process.EstadoNuevo);
        Assert.Equal("Corte solicitado", process.Motivo);
        Assert.Equal("Nota interna", process.Observacion);

        var audit = await dbContext.Auditorias.SingleAsync();
        Assert.Equal(8, audit.UsuarioAdministrativoId);
        Assert.Equal("SUMINISTRO.CANCELAR", audit.Accion);
        Assert.Equal("Suministro", audit.Entidad);
        Assert.Equal(10, audit.EntidadId);
        Assert.Equal(nameof(EstadoSuministro.Activo), audit.ValorAnterior);
        Assert.Equal(nameof(EstadoSuministro.Cancelado), audit.ValorNuevo);
    }

    [Fact]
    public async Task CancelAsync_RejectsCancelledAndInvalidInputs()
    {
        await using var dbContext = CreateContext();
        dbContext.UsuariosAdministrativos.Add(new UsuarioAdministrativo { Id = 8, NombreUsuario = "admin" });
        dbContext.Suministros.Add(new Suministro
        {
            Id = 10,
            SectorId = 1,
            Nis = "PAN-000010",
            CodigoQrToken = "token",
            DireccionReferencia = "Dirección",
            Estado = EstadoSuministro.Cancelado
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, "PAN-000011", "token-2");
        var conflict = await service.CancelAsync(10, new SuministroProcesoInput("Motivo", null), 8);
        var emptyReason = await service.CancelAsync(10, new SuministroProcesoInput("  ", null), 8);
        var longReason = await service.CancelAsync(10, new SuministroProcesoInput(new string('x', 251), null), 8);
        var longObservation = await service.CancelAsync(10, new SuministroProcesoInput("Motivo", new string('x', 1001)), 8);

        Assert.Equal(SuministroOperationError.Conflict, conflict.Error);
        Assert.Equal(SuministroOperationError.Invalid, emptyReason.Error);
        Assert.Equal(SuministroOperationError.Invalid, longReason.Error);
        Assert.Equal(SuministroOperationError.Invalid, longObservation.Error);
        Assert.Empty(await dbContext.ProcesosSuministro.ToListAsync());
    }

    [Fact]
    public async Task ReconnectAsync_ChangesCancelledSupplyAndRejectsActiveSupply()
    {
        await using var dbContext = CreateContext();
        dbContext.Sectores.Add(new Sector { Id = 1, Nombre = "Centro", Estado = EstadoRegistro.Activo });
        dbContext.UsuariosAdministrativos.Add(new UsuarioAdministrativo { Id = 8, NombreUsuario = "admin" });
        dbContext.Suministros.AddRange(
            new Suministro { Id = 10, SectorId = 1, Nis = "PAN-000010", CodigoQrToken = "token-10", DireccionReferencia = "Uno", Estado = EstadoSuministro.Cancelado },
            new Suministro { Id = 11, SectorId = 1, Nis = "PAN-000011", CodigoQrToken = "token-11", DireccionReferencia = "Dos", Estado = EstadoSuministro.Activo });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, "PAN-000012", "token-12");
        var reconnected = await service.ReconnectAsync(10, new SuministroProcesoInput("  Reconectar  ", null), 8);
        var conflict = await service.ReconnectAsync(11, new SuministroProcesoInput("Motivo", null), 8);

        Assert.True(reconnected.Succeeded);
        Assert.Equal(EstadoSuministro.Activo, reconnected.Value!.Estado);
        Assert.Equal(TipoProcesoSuministro.Reconexion, await dbContext.ProcesosSuministro.Select(p => p.TipoProceso).SingleAsync());
        Assert.Equal("SUMINISTRO.RECONECTAR", await dbContext.Auditorias.Select(a => a.Accion).SingleAsync());
        Assert.Equal(SuministroOperationError.Conflict, conflict.Error);
    }

    [Fact]
    public async Task GetProcesosAsync_ReturnsEmptyForExistingSupplyAndOrdersHistory()
    {
        await using var dbContext = CreateContext();
        dbContext.UsuariosAdministrativos.Add(new UsuarioAdministrativo { Id = 8, NombreUsuario = "admin" });
        dbContext.Suministros.Add(new Suministro
        {
            Id = 10,
            SectorId = 1,
            Nis = "PAN-000010",
            CodigoQrToken = "token",
            DireccionReferencia = "Dirección"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, "PAN-000011", "token-2");
        var empty = await service.GetProcesosAsync(10);
        dbContext.ProcesosSuministro.AddRange(
            new ProcesoSuministro { Id = 1, SuministroId = 10, TipoProceso = TipoProcesoSuministro.Cancelacion, EstadoAnterior = EstadoSuministro.Activo, EstadoNuevo = EstadoSuministro.Cancelado, Fecha = new DateTime(2026, 1, 1), UsuarioAdministrativoId = 8, Motivo = "Primero" },
            new ProcesoSuministro { Id = 2, SuministroId = 10, TipoProceso = TipoProcesoSuministro.Reconexion, EstadoAnterior = EstadoSuministro.Cancelado, EstadoNuevo = EstadoSuministro.Activo, Fecha = new DateTime(2026, 1, 1), UsuarioAdministrativoId = 8, Motivo = "Segundo" });
        await dbContext.SaveChangesAsync();

        var history = await service.GetProcesosAsync(10);
        var missing = await service.GetProcesosAsync(99);

        Assert.NotNull(empty);
        Assert.Empty(empty!);
        Assert.NotNull(history);
        Assert.Equal(new[] { 2, 1 }, history!.Select(p => p.ProcesoSuministroId));
        Assert.Equal("admin", history[0].Usuario);
        Assert.Null(missing);
    }

    private static PanyebarDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PanyebarDbContext(options);
    }

    private static SuministroService CreateService(
        PanyebarDbContext dbContext,
        string nis,
        string token)
    {
        return new SuministroService(
            dbContext,
            new FixedNisGenerator(nis),
            new FixedQrTokenGenerator(token));
    }

    private sealed class FixedNisGenerator : ISuministroNisGenerator
    {
        private readonly string _nis;

        public FixedNisGenerator(string nis)
        {
            _nis = nis;
        }

        public Task<string> GenerateAsync(CancellationToken cancellationToken = default) => Task.FromResult(_nis);
    }

    private sealed class FixedQrTokenGenerator : ISuministroQrTokenGenerator
    {
        private readonly string _token;

        public FixedQrTokenGenerator(string token)
        {
            _token = token;
        }

        public string Generate() => _token;
    }
}