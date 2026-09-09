using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Cuotas;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;

namespace Panyebar.Security.Tests;

public sealed class CuotaServiceTests
{
    [Fact]
    public async Task CreateAndGetAllAsync_NormalizesPersistsAndAuditsActiveFee()
    {
        await using var context = CreateContext();
        var user = AddUser(context);
        var service = new CuotaService(context);

        var result = await service.CreateAsync(
            new CuotaInput(
                "  Cuota ordinaria  ",
                "  Aporte anual  ",
                30m,
                PeriodicidadCuota.Anual,
                new DateTime(2026, 1, 1),
                null),
            user.Id);

        Assert.True(result.Succeeded);
        Assert.Equal("Cuota ordinaria", result.Value!.Nombre);
        Assert.Equal("Aporte anual", result.Value.Descripcion);
        Assert.Equal(EstadoRegistro.Activo, result.Value.Estado);
        Assert.Equal(result.Value, Assert.Single(await service.GetAllAsync()));
        var audit = await context.Auditorias.SingleAsync();
        Assert.Equal(user.Id, audit.UsuarioAdministrativoId);
        Assert.Equal("CUOTA.CREAR", audit.Accion);
        Assert.Equal(result.Value.Id, audit.EntidadId);
    }

    [Theory]
    [InlineData(null, 30, 1)]
    [InlineData("", 30, 1)]
    [InlineData("Nombre", 30, 99)]
    public async Task CreateAsync_RejectsInvalidRequiredValues(string? name, int amount, int periodicity)
    {
        await using var context = CreateContext();
        var user = AddUser(context);
        var service = new CuotaService(context);

        var result = await service.CreateAsync(
            new CuotaInput(name, null, amount, (PeriodicidadCuota)periodicity, new DateTime(2026, 1, 1), null),
            user.Id);

        Assert.Equal(CuotaOperationError.Invalid, result.Error);
        Assert.Empty(context.Cuotas);
        Assert.Empty(context.Auditorias);
    }

    [Fact]
    public async Task CreateAsync_RejectsNameDescriptionAndAmountOutsidePersistenceLimits()
    {
        await using var context = CreateContext();
        var user = AddUser(context);
        var service = new CuotaService(context);

        var longName = await service.CreateAsync(Input(new string('N', 101)), user.Id);
        var longDescription = await service.CreateAsync(
            Input("Nombre") with { Descripcion = new string('D', 501) }, user.Id);
        var excessiveScale = await service.CreateAsync(Input("Nombre") with { Monto = 10.001m }, user.Id);
        var excessiveMagnitude = await service.CreateAsync(Input("Nombre") with { Monto = decimal.MaxValue }, user.Id);
        var invalidDates = await service.CreateAsync(
            Input("Nombre") with { FechaFinVigencia = new DateTime(2025, 12, 31) }, user.Id);

        Assert.All(
            new[] { longName, longDescription, excessiveScale, excessiveMagnitude, invalidDates },
            result => Assert.Equal(CuotaOperationError.Invalid, result.Error));
        Assert.Empty(context.Cuotas);
    }

    [Fact]
    public async Task UpdateAndSetEstadoAsync_ModifyConfigurationAndCreateAudits()
    {
        await using var context = CreateContext();
        var user = AddUser(context);
        var fee = AddFee(context, "Anterior", 30m, PeriodicidadCuota.Anual);
        var service = new CuotaService(context);

        var updated = await service.UpdateAsync(
            fee.Id,
            new CuotaInput("Mensual", null, 40m, PeriodicidadCuota.Mensual, new DateTime(2026, 2, 1), null),
            user.Id);
        var disabled = await service.SetEstadoAsync(fee.Id, EstadoRegistro.Inactivo, user.Id);

        Assert.True(updated.Succeeded);
        Assert.Equal(40m, updated.Value!.Monto);
        Assert.Equal(PeriodicidadCuota.Mensual, updated.Value.Periodicidad);
        Assert.Equal(EstadoRegistro.Inactivo, disabled.Value!.Estado);
        Assert.Equal(
            new[] { "CUOTA.ACTUALIZAR", "CUOTA.ESTADO.ACTUALIZAR" },
            await context.Auditorias.OrderBy(a => a.Id).Select(a => a.Accion).ToArrayAsync());
    }

    [Fact]
    public async Task Mutations_RejectUnknownAdministrativeUserAndMissingFee()
    {
        await using var context = CreateContext();
        var service = new CuotaService(context);

        var create = await service.CreateAsync(Input("Cuota"), 999);
        AddUser(context);
        var update = await service.UpdateAsync(999, Input("Cuota"), 1);
        var state = await service.SetEstadoAsync(999, EstadoRegistro.Activo, 1);

        Assert.Equal(CuotaOperationError.Invalid, create.Error);
        Assert.Equal(CuotaOperationError.NotFound, update.Error);
        Assert.Equal(CuotaOperationError.NotFound, state.Error);
    }

    private static CuotaInput Input(string name) => new(
        name,
        "Descripción",
        30m,
        PeriodicidadCuota.Anual,
        new DateTime(2026, 1, 1),
        null);

    private static PanyebarDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PanyebarDbContext(options);
    }

    private static UsuarioAdministrativo AddUser(PanyebarDbContext context)
    {
        var user = new UsuarioAdministrativo
        {
            Id = 1,
            NombreUsuario = "admin",
            PasswordHash = "hash",
            Estado = EstadoRegistro.Activo
        };
        context.UsuariosAdministrativos.Add(user);
        context.SaveChanges();
        return user;
    }

    private static Cuota AddFee(
        PanyebarDbContext context,
        string name,
        decimal amount,
        PeriodicidadCuota periodicity)
    {
        var fee = new Cuota
        {
            Nombre = name,
            Monto = amount,
            Periodicidad = periodicity,
            FechaInicioVigencia = new DateTime(2026, 1, 1),
            Estado = EstadoRegistro.Activo
        };
        context.Cuotas.Add(fee);
        context.SaveChanges();
        return fee;
    }
}
