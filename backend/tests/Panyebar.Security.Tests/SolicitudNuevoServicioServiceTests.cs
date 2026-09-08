using Microsoft.EntityFrameworkCore;
using Panyebar.Application.SolicitudesNuevoServicio;
using Panyebar.Application.Suministros;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;
using Panyebar.Infrastructure.Security;

namespace Panyebar.Security.Tests;

public sealed class SolicitudNuevoServicioServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesPendingRequestAndNormalizesValues()
    {
        await using var dbContext = CreateContext();
        SeedBaseData(dbContext);
        var service = CreateService(dbContext, "PAN-000777", "qr-777");

        var result = await service.CreateAsync(
            new SolicitudNuevoServicioInput(4, 1, "  Calle Nueva  ", "  Solicitud inicial  "),
            8);

        Assert.True(result.Succeeded);
        Assert.Equal(EstadoSolicitudNuevoServicio.Pendiente, result.Value!.Estado);
        Assert.Equal("Calle Nueva", result.Value.DireccionReferencia);
        Assert.Equal("Solicitud inicial", result.Value.Observacion);
        Assert.Null(result.Value.FechaResolucion);
        Assert.Null(result.Value.UsuarioResolucionId);
        Assert.Null(result.Value.SuministroId);
        Assert.Equal("Ana Pérez", result.Value.PersonaSolicitante);
        Assert.Equal("Centro", result.Value.SectorNombre);

        var audit = await dbContext.Auditorias.SingleAsync();
        Assert.Equal("SOLICITUD_NUEVO_SERVICIO.CREAR", audit.Accion);
        Assert.Equal("Pendiente", audit.ValorNuevo);
    }

    [Fact]
    public async Task CreateAsync_RejectsInvalidReferencesDataAndDuplicatePendingRequest()
    {
        await using var dbContext = CreateContext();
        SeedBaseData(dbContext);
        dbContext.Personas.Add(new Persona { Id = 5, Nombres = "Luis", Apellidos = "Gómez", Estado = EstadoRegistro.Inactivo });
        dbContext.Sectores.Add(new Sector { Id = 2, Nombre = "Norte", Estado = EstadoRegistro.Inactivo });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, "PAN-000777", "qr-777");

        var missingPerson = await service.CreateAsync(new SolicitudNuevoServicioInput(99, 1, "Dirección", null), 8);
        var inactivePerson = await service.CreateAsync(new SolicitudNuevoServicioInput(5, 1, "Dirección", null), 8);
        var inactiveSector = await service.CreateAsync(new SolicitudNuevoServicioInput(4, 2, "Dirección", null), 8);
        var emptyAddress = await service.CreateAsync(new SolicitudNuevoServicioInput(4, 1, "  ", null), 8);
        var longAddress = await service.CreateAsync(new SolicitudNuevoServicioInput(4, 1, new string('x', 501), null), 8);
        var longObservation = await service.CreateAsync(new SolicitudNuevoServicioInput(4, 1, "Dirección", new string('x', 1001)), 8);
        var invalidUser = await service.CreateAsync(new SolicitudNuevoServicioInput(4, 1, "Dirección", null), 99);
        var valid = await service.CreateAsync(new SolicitudNuevoServicioInput(4, 1, "Dirección", null), 8);
        var duplicate = await service.CreateAsync(new SolicitudNuevoServicioInput(4, 1, "Otra dirección", null), 8);

        Assert.All(
            new[] { missingPerson, inactivePerson, inactiveSector, emptyAddress, longAddress, longObservation, invalidUser },
            result => Assert.Equal(SolicitudNuevoServicioOperationError.Invalid, result.Error));
        Assert.True(valid.Succeeded);
        Assert.Equal(SolicitudNuevoServicioOperationError.Conflict, duplicate.Error);
        Assert.Equal(1, await dbContext.SolicitudesNuevoServicio.CountAsync());
    }

    [Fact]
    public async Task GetAllAsync_OrdersPendingFirstThenDateAndId()
    {
        await using var dbContext = CreateContext();
        SeedBaseData(dbContext);
        dbContext.Personas.Add(new Persona { Id = 5, Nombres = "Luis", Apellidos = "Gómez" });
        dbContext.SolicitudesNuevoServicio.AddRange(
            new SolicitudNuevoServicio { Id = 1, PersonaSolicitanteId = 4, SectorId = 1, DireccionReferencia = "Uno", FechaSolicitud = new DateTime(2026, 1, 1), Estado = EstadoSolicitudNuevoServicio.Aprobada },
            new SolicitudNuevoServicio { Id = 2, PersonaSolicitanteId = 5, SectorId = 1, DireccionReferencia = "Dos", FechaSolicitud = new DateTime(2026, 1, 2), Estado = EstadoSolicitudNuevoServicio.Pendiente },
            new SolicitudNuevoServicio { Id = 3, PersonaSolicitanteId = 4, SectorId = 1, DireccionReferencia = "Tres", FechaSolicitud = new DateTime(2026, 1, 3), Estado = EstadoSolicitudNuevoServicio.Pendiente });
        await dbContext.SaveChangesAsync();

        var result = await CreateService(dbContext, "PAN-000777", "qr-777").GetAllAsync();

        Assert.Equal(new[] { 3, 2, 1 }, result.Select(request => request.SolicitudNuevoServicioId));
        Assert.Null(await CreateService(dbContext, "PAN-000777", "qr-777").GetByIdAsync(99));
    }

    [Fact]
    public async Task ApproveAsync_CreatesOneActiveSupplyAndInitialResponsibleWithAudit()
    {
        await using var dbContext = CreateContext();
        SeedBaseData(dbContext);
        var service = CreateService(dbContext, "PAN-000777", "qr-777");
        var created = await service.CreateAsync(new SolicitudNuevoServicioInput(4, 1, "Dirección nueva", null), 8);

        var result = await service.ApproveAsync(
            created.Value!.SolicitudNuevoServicioId,
            new SolicitudNuevoServicioResolutionInput("  Aprobado por administración  "),
            8);

        Assert.True(result.Succeeded);
        Assert.Equal(EstadoSolicitudNuevoServicio.Aprobada, result.Value!.Estado);
        Assert.Equal("Aprobado por administración", result.Value.Observacion);
        Assert.Equal(1, await dbContext.Suministros.CountAsync());
        var supply = await dbContext.Suministros.SingleAsync();
        Assert.Equal("PAN-000777", supply.Nis);
        Assert.Equal("qr-777", supply.CodigoQrToken);
        Assert.Equal(EstadoSuministro.Activo, supply.Estado);
        Assert.Equal(supply.Id, result.Value.SuministroId);
        Assert.Equal(1, await dbContext.PersonaSuministros.CountAsync());
        var relationship = await dbContext.PersonaSuministros.SingleAsync();
        Assert.Equal(4, relationship.PersonaId);
        Assert.Equal(EstadoRelacionSuministro.Vigente, relationship.Estado);
        Assert.Null(relationship.FechaFin);
        Assert.Equal(8, result.Value.UsuarioResolucionId);
        Assert.NotNull(result.Value.FechaResolucion);
        Assert.Equal("PAN-000777", result.Value.Nis);
        Assert.DoesNotContain("CodigoQrToken", typeof(SolicitudNuevoServicioDto).GetProperties().Select(property => property.Name));

        var approvalAudit = await dbContext.Auditorias.SingleAsync(a => a.Accion == "SOLICITUD_NUEVO_SERVICIO.APROBAR");
        Assert.Equal("Pendiente", approvalAudit.ValorAnterior);
        Assert.Equal("Aprobada", approvalAudit.ValorNuevo);
    }

    [Fact]
    public async Task ApproveAsync_RejectsResolvedRequestAndDoesNotCreateAnotherSupply()
    {
        await using var dbContext = CreateContext();
        SeedBaseData(dbContext);
        var service = CreateService(dbContext, "PAN-000777", "qr-777");
        var created = await service.CreateAsync(new SolicitudNuevoServicioInput(4, 1, "Dirección", null), 8);
        await service.ApproveAsync(created.Value!.SolicitudNuevoServicioId, new SolicitudNuevoServicioResolutionInput(null), 8);

        var secondAttempt = await service.ApproveAsync(created.Value.SolicitudNuevoServicioId, new SolicitudNuevoServicioResolutionInput(null), 8);
        var rejectedAttempt = await service.RejectAsync(created.Value.SolicitudNuevoServicioId, new SolicitudNuevoServicioResolutionInput("No procede"), 8);

        Assert.Equal(SolicitudNuevoServicioOperationError.Conflict, secondAttempt.Error);
        Assert.Equal(SolicitudNuevoServicioOperationError.Conflict, rejectedAttempt.Error);
        Assert.Equal(1, await dbContext.Suministros.CountAsync());
    }

    [Fact]
    public async Task RejectAsync_ResolvesWithoutCreatingSupplyOrResponsible()
    {
        await using var dbContext = CreateContext();
        SeedBaseData(dbContext);
        var service = CreateService(dbContext, "PAN-000777", "qr-777");
        var created = await service.CreateAsync(new SolicitudNuevoServicioInput(4, 1, "Dirección", "Original"), 8);

        var result = await service.RejectAsync(
            created.Value!.SolicitudNuevoServicioId,
            new SolicitudNuevoServicioResolutionInput("  Rechazada por falta de factibilidad  "),
            8);

        Assert.True(result.Succeeded);
        Assert.Equal(EstadoSolicitudNuevoServicio.Rechazada, result.Value!.Estado);
        Assert.Equal("Rechazada por falta de factibilidad", result.Value.Observacion);
        Assert.Equal(8, result.Value.UsuarioResolucionId);
        Assert.NotNull(result.Value.FechaResolucion);
        Assert.Null(result.Value.SuministroId);
        Assert.Empty(await dbContext.Suministros.ToListAsync());
        Assert.Empty(await dbContext.PersonaSuministros.ToListAsync());
        Assert.NotNull(await dbContext.Auditorias.SingleOrDefaultAsync(a => a.Accion == "SOLICITUD_NUEVO_SERVICIO.RECHAZAR"));
    }

    [Fact]
    public async Task ResolveAsync_RejectsUnknownUserAndOverlongObservation()
    {
        await using var dbContext = CreateContext();
        SeedBaseData(dbContext);
        var service = CreateService(dbContext, "PAN-000777", "qr-777");
        var created = await service.CreateAsync(new SolicitudNuevoServicioInput(4, 1, "Dirección", null), 8);

        var unknownUser = await service.ApproveAsync(created.Value!.SolicitudNuevoServicioId, new SolicitudNuevoServicioResolutionInput(null), 99);
        var overlongObservation = await service.RejectAsync(created.Value.SolicitudNuevoServicioId, new SolicitudNuevoServicioResolutionInput(new string('x', 1001)), 8);
        var request = await dbContext.SolicitudesNuevoServicio.SingleAsync();

        Assert.Equal(SolicitudNuevoServicioOperationError.Invalid, unknownUser.Error);
        Assert.Equal(SolicitudNuevoServicioOperationError.Invalid, overlongObservation.Error);
        Assert.Equal(EstadoSolicitudNuevoServicio.Pendiente, request.Estado);
        Assert.Empty(await dbContext.Suministros.ToListAsync());
    }

    private static void SeedBaseData(PanyebarDbContext dbContext)
    {
        dbContext.Sectores.Add(new Sector { Id = 1, Nombre = "Centro", Estado = EstadoRegistro.Activo });
        dbContext.Personas.Add(new Persona { Id = 4, Nombres = "Ana", Apellidos = "Pérez", Estado = EstadoRegistro.Activo });
        dbContext.UsuariosAdministrativos.Add(new UsuarioAdministrativo { Id = 8, NombreUsuario = "admin", Estado = EstadoRegistro.Activo });
        dbContext.SaveChanges();
    }

    private static PanyebarDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PanyebarDbContext(options);
    }

    private static SolicitudNuevoServicioService CreateService(
        PanyebarDbContext dbContext,
        string nis,
        string token)
    {
        return new SolicitudNuevoServicioService(
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