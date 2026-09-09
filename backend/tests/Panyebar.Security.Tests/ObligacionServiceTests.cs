using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Obligaciones;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;

namespace Panyebar.Security.Tests;

public sealed class ObligacionServiceTests
{
    [Theory]
    [InlineData(PeriodicidadCuota.Anual, "2026")]
    [InlineData(PeriodicidadCuota.Mensual, "2026-01")]
    public async Task GenerateFromCuotaAsync_CreatesSupplyObligationWithFrozenValues(
        PeriodicidadCuota periodicity,
        string period)
    {
        await using var context = CreateContext();
        var setup = AddReferences(context, periodicity, 30m);
        var service = new ObligacionService(context);

        var result = await service.GenerateFromCuotaAsync(
            new GenerarObligacionCuotaInput(setup.Fee.Id, setup.Supply.Id, period, DateTime.UtcNow.AddDays(10)),
            setup.User.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(setup.Supply.Id, result.Value!.SuministroId);
        Assert.Null(result.Value.PersonaId);
        Assert.Equal(setup.Fee.Id, result.Value.CuotaId);
        Assert.Equal(OrigenObligacion.CuotaOrdinaria, result.Value.Origen);
        Assert.Equal(setup.Fee.Nombre, result.Value.Concepto);
        Assert.Equal(30m, result.Value.Monto);
        Assert.Equal(period, result.Value.Periodo);
        Assert.Equal(EstadoObligacion.Pendiente, result.Value.Estado);
        Assert.False(result.Value.EsMorosa);
        Assert.Equal("OBLIGACION.GENERAR.DESDE_CUOTA", (await context.Auditorias.SingleAsync()).Accion);
    }

    [Theory]
    [InlineData(PeriodicidadCuota.Anual, null)]
    [InlineData(PeriodicidadCuota.Anual, "26")]
    [InlineData(PeriodicidadCuota.Anual, "2026-01")]
    [InlineData(PeriodicidadCuota.Mensual, "2026")]
    [InlineData(PeriodicidadCuota.Mensual, "2026-1")]
    [InlineData(PeriodicidadCuota.Mensual, "2026-13")]
    [InlineData(PeriodicidadCuota.Mensual, "2026-00")]
    public async Task GenerateFromCuotaAsync_RejectsInvalidOrIncompatiblePeriod(
        PeriodicidadCuota periodicity,
        string? period)
    {
        await using var context = CreateContext();
        var setup = AddReferences(context, periodicity, 30m);
        var service = new ObligacionService(context);

        var result = await service.GenerateFromCuotaAsync(
            new GenerarObligacionCuotaInput(setup.Fee.Id, setup.Supply.Id, period, null),
            setup.User.Id);

        Assert.Equal(ObligacionOperationError.Invalid, result.Error);
        Assert.Empty(context.Obligaciones);
    }

    [Fact]
    public async Task GenerateFromCuotaAsync_RejectsDuplicateButAllowsReplacementAfterAnnulment()
    {
        await using var context = CreateContext();
        var setup = AddReferences(context, PeriodicidadCuota.Anual, 30m);
        var service = new ObligacionService(context);
        var input = new GenerarObligacionCuotaInput(setup.Fee.Id, setup.Supply.Id, "2026", null);

        var first = await service.GenerateFromCuotaAsync(input, setup.User.Id);
        var duplicate = await service.GenerateFromCuotaAsync(input, setup.User.Id);
        var annulled = await service.AnnulAsync(first.Value!.Id, new AnularObligacionInput("Corrección"), setup.User.Id);
        var replacement = await service.GenerateFromCuotaAsync(input, setup.User.Id);

        Assert.Equal(ObligacionOperationError.Conflict, duplicate.Error);
        Assert.Equal(EstadoObligacion.Anulada, annulled.Value!.Estado);
        Assert.True(replacement.Succeeded);
        Assert.Equal(2, await context.Obligaciones.CountAsync());
        Assert.Equal(1, await context.Obligaciones.CountAsync(o => o.Estado == EstadoObligacion.Anulada));
        Assert.Equal(1, await context.Obligaciones.CountAsync(o => o.Estado == EstadoObligacion.Pendiente));
    }

    [Fact]
    public async Task GeneratedAmount_RemainsFrozenWhenFeeChanges()
    {
        await using var context = CreateContext();
        var setup = AddReferences(context, PeriodicidadCuota.Anual, 30m);
        var service = new ObligacionService(context);
        var generated = await service.GenerateFromCuotaAsync(
            new GenerarObligacionCuotaInput(setup.Fee.Id, setup.Supply.Id, "2026", null),
            setup.User.Id);

        setup.Fee.Monto = 40m;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var historical = await service.GetByIdAsync(generated.Value!.Id);

        Assert.Equal(40m, (await context.Cuotas.SingleAsync()).Monto);
        Assert.Equal(30m, historical!.Monto);
    }

    [Fact]
    public async Task GetAllAsync_CalculatesMorosityOnlyForExpiredPendingObligation()
    {
        await using var context = CreateContext();
        var setup = AddReferences(context, PeriodicidadCuota.Anual, 30m);
        var now = DateTime.UtcNow;
        context.Obligaciones.AddRange(
            Obligation(setup, "2023", EstadoObligacion.Pendiente, now.AddDays(-1)),
            Obligation(setup, "2024", EstadoObligacion.Pendiente, null),
            Obligation(setup, "2025", EstadoObligacion.Pagada, now.AddDays(-1)),
            Obligation(setup, "2026", EstadoObligacion.Anulada, now.AddDays(-1)));
        await context.SaveChangesAsync();
        var service = new ObligacionService(context);

        var result = await service.GetAllAsync();

        Assert.True(result.Single(o => o.Periodo == "2023").EsMorosa);
        Assert.False(result.Single(o => o.Periodo == "2024").EsMorosa);
        Assert.False(result.Single(o => o.Periodo == "2025").EsMorosa);
        Assert.False(result.Single(o => o.Periodo == "2026").EsMorosa);
    }

    [Fact]
    public async Task AnnulAsync_ChangesOnlyPendingStatePersistsReasonAndDoesNotDelete()
    {
        await using var context = CreateContext();
        var setup = AddReferences(context, PeriodicidadCuota.Anual, 30m);
        var obligation = Obligation(setup, "2026", EstadoObligacion.Pendiente, null);
        context.Obligaciones.Add(obligation);
        await context.SaveChangesAsync();
        var service = new ObligacionService(context);

        var result = await service.AnnulAsync(obligation.Id, new AnularObligacionInput("  Cobro emitido por error  "), setup.User.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(EstadoObligacion.Anulada, result.Value!.Estado);
        Assert.Equal(1, await context.Obligaciones.CountAsync());
        var audit = await context.Auditorias.SingleAsync();
        Assert.Equal(setup.User.Id, audit.UsuarioAdministrativoId);
        Assert.Equal("OBLIGACION.ANULAR", audit.Accion);
        Assert.Contains("Motivo:Cobro emitido por error", audit.ValorNuevo);
        Assert.InRange(audit.Fecha, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task AnnulAsync_RejectsPaidSecondAnnulmentEmptyReasonAndUnknownUser()
    {
        await using var context = CreateContext();
        var setup = AddReferences(context, PeriodicidadCuota.Anual, 30m);
        var pending = Obligation(setup, "2025", EstadoObligacion.Pendiente, null);
        var paid = Obligation(setup, "2026", EstadoObligacion.Pagada, null);
        context.Obligaciones.AddRange(pending, paid);
        await context.SaveChangesAsync();
        var service = new ObligacionService(context);

        var emptyReason = await service.AnnulAsync(pending.Id, new AnularObligacionInput("  "), setup.User.Id);
        var paidResult = await service.AnnulAsync(paid.Id, new AnularObligacionInput("Error"), setup.User.Id);
        var firstAnnulment = await service.AnnulAsync(pending.Id, new AnularObligacionInput("Error"), setup.User.Id);
        var secondAnnulment = await service.AnnulAsync(pending.Id, new AnularObligacionInput("Otra vez"), setup.User.Id);
        var unknownUser = await service.AnnulAsync(paid.Id, new AnularObligacionInput("Error"), 999);

        Assert.Equal(ObligacionOperationError.Invalid, emptyReason.Error);
        Assert.Equal(ObligacionOperationError.Conflict, paidResult.Error);
        Assert.True(firstAnnulment.Succeeded);
        Assert.Equal(ObligacionOperationError.Conflict, secondAnnulment.Error);
        Assert.Equal(ObligacionOperationError.Invalid, unknownUser.Error);
    }

    [Fact]
    public async Task GenerateFromCuotaAsync_RejectsMissingReferencesAndUnknownAdministrativeUser()
    {
        await using var context = CreateContext();
        var setup = AddReferences(context, PeriodicidadCuota.Anual, 30m);
        var service = new ObligacionService(context);

        var missingFee = await service.GenerateFromCuotaAsync(
            new GenerarObligacionCuotaInput(999, setup.Supply.Id, "2026", null), setup.User.Id);
        var missingSupply = await service.GenerateFromCuotaAsync(
            new GenerarObligacionCuotaInput(setup.Fee.Id, 999, "2026", null), setup.User.Id);
        var unknownUser = await service.GenerateFromCuotaAsync(
            new GenerarObligacionCuotaInput(setup.Fee.Id, setup.Supply.Id, "2026", null), 999);

        Assert.Equal(ObligacionOperationError.NotFound, missingFee.Error);
        Assert.Equal(ObligacionOperationError.NotFound, missingSupply.Error);
        Assert.Equal(ObligacionOperationError.Invalid, unknownUser.Error);
    }

    private static Obligacion Obligation(
        (UsuarioAdministrativo User, Cuota Fee, Suministro Supply) setup,
        string period,
        EstadoObligacion state,
        DateTime? dueDate) => new()
        {
            CuotaId = setup.Fee.Id,
            SuministroId = setup.Supply.Id,
            PersonaId = null,
            Origen = OrigenObligacion.CuotaOrdinaria,
            Concepto = setup.Fee.Nombre,
            Monto = setup.Fee.Monto,
            Periodo = period,
            FechaGeneracion = DateTime.UtcNow.AddDays(-10),
            FechaVencimiento = dueDate,
            Estado = state
        };

    private static (UsuarioAdministrativo User, Cuota Fee, Suministro Supply) AddReferences(
        PanyebarDbContext context,
        PeriodicidadCuota periodicity,
        decimal amount)
    {
        var user = new UsuarioAdministrativo
        {
            NombreUsuario = "admin",
            PasswordHash = "hash",
            Estado = EstadoRegistro.Activo
        };
        var fee = new Cuota
        {
            Nombre = "Cuota ordinaria",
            Monto = amount,
            Periodicidad = periodicity,
            FechaInicioVigencia = new DateTime(2026, 1, 1),
            Estado = EstadoRegistro.Activo
        };
        var supply = new Suministro
        {
            SectorId = 1,
            Nis = "PAN-000001",
            CodigoQrToken = "token",
            DireccionReferencia = "Centro",
            Estado = EstadoSuministro.Activo
        };
        context.AddRange(user, fee, supply);
        context.SaveChanges();
        return (user, fee, supply);
    }

    private static PanyebarDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PanyebarDbContext(options);
    }
}
