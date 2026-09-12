using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Pagos;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;

namespace Panyebar.Security.Tests;

public sealed class PagoServiceTests
{
    [Fact]
    public async Task RegisterAsync_PaysSingleSupplyObligationAndCreatesAudit()
    {
        await using var context = CreateContext();
        var setup = AddReferences(context);

        var obligation = SupplyObligation(setup.Supply.Id, 30m, "2026");
        context.Obligaciones.Add(obligation);
        await context.SaveChangesAsync();

        var service = new PagoService(context);

        var result = await service.RegisterAsync(
            new RegistrarPagoInput(
                30m,
                "Pago de obligaciones",
                new[] { obligation.Id }),
            setup.User.Id);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal("PAG-000001", result.Value!.NumeroComprobante);
        Assert.Equal(30m, result.Value.Monto);
        Assert.Equal("Suministro", result.Value.Titular.Tipo);
        Assert.Equal(setup.Supply.Id, result.Value.Titular.Id);
        Assert.Equal(setup.Supply.Nis, result.Value.Titular.Nis);

        Assert.Equal(
            EstadoObligacion.Pagada,
            (await context.Obligaciones.FindAsync(obligation.Id))!.Estado);

        var payment = Assert.Single(context.Pagos);
        var application = Assert.Single(context.AplicacionesPago);
        Assert.Equal(payment.Id, application.PagoId);
        Assert.Equal(obligation.Id, application.ObligacionId);

        var audit = Assert.Single(
            context.Auditorias.Where(a => a.Accion == "PAGO.REGISTRAR"));

        Assert.Equal(payment.Id, audit.EntidadId);
        Assert.Equal(setup.User.Id, audit.UsuarioAdministrativoId);
    }

    [Fact]
    public async Task RegisterAsync_PaysMultipleCompleteObligationsOfSameSupply()
    {
        await using var context = CreateContext();
        var setup = AddReferences(context);

        var first = SupplyObligation(setup.Supply.Id, 30m, "2025");
        var second = SupplyObligation(setup.Supply.Id, 40m, "2026");

        context.Obligaciones.AddRange(first, second);
        await context.SaveChangesAsync();

        var service = new PagoService(context);

        var result = await service.RegisterAsync(
            new RegistrarPagoInput(
                70m,
                "Pago de obligaciones",
                new[] { first.Id, second.Id }),
            setup.User.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Value!.Obligaciones.Count);
        Assert.Equal(2, context.AplicacionesPago.Count());
        Assert.All(
            context.Obligaciones,
            obligation =>
                Assert.Equal(
                    EstadoObligacion.Pagada,
                    obligation.Estado));
    }

    [Theory]
    [InlineData(29)]
    [InlineData(31)]
    public async Task RegisterAsync_RejectsAmountDifferentFromExactObligationSum(
        int amount)
    {
        await using var context = CreateContext();
        var setup = AddReferences(context);

        var obligation = SupplyObligation(setup.Supply.Id, 30m, "2026");
        context.Obligaciones.Add(obligation);
        await context.SaveChangesAsync();

        var service = new PagoService(context);

        var result = await service.RegisterAsync(
            new RegistrarPagoInput(
                amount,
                "Pago",
                new[] { obligation.Id }),
            setup.User.Id);

        Assert.Equal(PagoOperationError.Invalid, result.Error);
        Assert.Empty(context.Pagos);
        Assert.Empty(context.AplicacionesPago);
        Assert.Equal(
            EstadoObligacion.Pendiente,
            obligation.Estado);
    }

    [Fact]
    public async Task RegisterAsync_RejectsDuplicateObligationIds()
    {
        await using var context = CreateContext();
        var setup = AddReferences(context);

        var obligation = SupplyObligation(setup.Supply.Id, 30m, "2026");
        context.Obligaciones.Add(obligation);
        await context.SaveChangesAsync();

        var service = new PagoService(context);

        var result = await service.RegisterAsync(
            new RegistrarPagoInput(
                60m,
                "Pago",
                new[] { obligation.Id, obligation.Id }),
            setup.User.Id);

        Assert.Equal(PagoOperationError.Invalid, result.Error);
        Assert.Empty(context.Pagos);
    }

    [Theory]
    [InlineData(EstadoObligacion.Pagada)]
    [InlineData(EstadoObligacion.Anulada)]
    public async Task RegisterAsync_RejectsNonPendingObligation(
        EstadoObligacion state)
    {
        await using var context = CreateContext();
        var setup = AddReferences(context);

        var obligation = SupplyObligation(setup.Supply.Id, 30m, "2026");
        obligation.Estado = state;

        context.Obligaciones.Add(obligation);
        await context.SaveChangesAsync();

        var service = new PagoService(context);

        var result = await service.RegisterAsync(
            new RegistrarPagoInput(
                30m,
                "Pago",
                new[] { obligation.Id }),
            setup.User.Id);

        Assert.Equal(PagoOperationError.Conflict, result.Error);
        Assert.Empty(context.Pagos);
    }

    [Fact]
    public async Task RegisterAsync_RejectsMissingObligationAndUnknownUser()
    {
        await using var context = CreateContext();
        var setup = AddReferences(context);
        var service = new PagoService(context);

        var missing = await service.RegisterAsync(
            new RegistrarPagoInput(30m, "Pago", new[] { 999 }),
            setup.User.Id);

        Assert.Equal(PagoOperationError.NotFound, missing.Error);

        var obligation = SupplyObligation(setup.Supply.Id, 30m, "2026");
        context.Obligaciones.Add(obligation);
        await context.SaveChangesAsync();

        var unknownUser = await service.RegisterAsync(
            new RegistrarPagoInput(30m, "Pago", new[] { obligation.Id }),
            999);

        Assert.Equal(PagoOperationError.Invalid, unknownUser.Error);
        Assert.Empty(context.Pagos);
    }

    [Fact]
    public async Task RegisterAsync_RejectsDifferentSupplyOwners()
    {
        await using var context = CreateContext();
        var setup = AddReferences(context);

        var otherSupply = new Suministro
        {
            SectorId = 1,
            Nis = "PAN-000002",
            CodigoQrToken = "token-2",
            DireccionReferencia = "Otro",
            Estado = EstadoSuministro.Activo
        };

        context.Suministros.Add(otherSupply);
        await context.SaveChangesAsync();

        var first = SupplyObligation(setup.Supply.Id, 30m, "2026");
        var second = SupplyObligation(otherSupply.Id, 30m, "2026");

        context.Obligaciones.AddRange(first, second);
        await context.SaveChangesAsync();

        var service = new PagoService(context);

        var result = await service.RegisterAsync(
            new RegistrarPagoInput(
                60m,
                "Pago",
                new[] { first.Id, second.Id }),
            setup.User.Id);

        Assert.Equal(PagoOperationError.Conflict, result.Error);
        Assert.Empty(context.Pagos);
    }

    [Fact]
    public async Task RegisterAsync_SupportsPersonalObligationWithoutSupply()
    {
        await using var context = CreateContext();
        var setup = AddReferences(context);

        var obligation = new Obligacion
        {
            PersonaId = setup.Person.Id,
            SuministroId = null,
            CuotaId = null,
            Origen = OrigenObligacion.Jornada,
            Concepto = "Ausencia jornada",
            Monto = 30m,
            Periodo = null,
            FechaGeneracion = DateTime.UtcNow,
            Estado = EstadoObligacion.Pendiente
        };

        context.Obligaciones.Add(obligation);
        await context.SaveChangesAsync();

        var service = new PagoService(context);

        var result = await service.RegisterAsync(
            new RegistrarPagoInput(
                30m,
                "Pago de obligación personal",
                new[] { obligation.Id }),
            setup.User.Id);

        Assert.True(result.Succeeded);
        Assert.Equal("Persona", result.Value!.Titular.Tipo);
        Assert.Equal(setup.Person.Id, result.Value.Titular.Id);
        Assert.Null(result.Value.Titular.Nis);
    }

    [Fact]
    public async Task RegisterAsync_RejectsMixedPersonAndSupplyOwners()
    {
        await using var context = CreateContext();
        var setup = AddReferences(context);

        var supplyObligation =
            SupplyObligation(setup.Supply.Id, 30m, "2026");

        var personalObligation = new Obligacion
        {
            PersonaId = setup.Person.Id,
            SuministroId = null,
            Origen = OrigenObligacion.Jornada,
            Concepto = "Ausencia",
            Monto = 30m,
            FechaGeneracion = DateTime.UtcNow,
            Estado = EstadoObligacion.Pendiente
        };

        context.Obligaciones.AddRange(
            supplyObligation,
            personalObligation);

        await context.SaveChangesAsync();

        var service = new PagoService(context);

        var result = await service.RegisterAsync(
            new RegistrarPagoInput(
                60m,
                "Pago",
                new[] { supplyObligation.Id, personalObligation.Id }),
            setup.User.Id);

        Assert.Equal(PagoOperationError.Conflict, result.Error);
        Assert.Empty(context.Pagos);
    }

    [Fact]
    public async Task RegisterAsync_UsesCurrentSupplyResponsibleInsteadOfHistoricalRelation()
    {
        await using var context = CreateContext();
        var setup = AddReferences(context);

        var historicalPerson = new Persona
        {
            Nombres = "Responsable",
            Apellidos = "Anterior",
            Identificacion = "9876543210101",
            Estado = EstadoRegistro.Activo
        };

        context.Personas.Add(historicalPerson);
        await context.SaveChangesAsync();

        var currentRelation = await context.PersonaSuministros
            .SingleAsync(ps =>
                ps.SuministroId == setup.Supply.Id &&
                ps.Estado == EstadoRelacionSuministro.Vigente);

        currentRelation.FechaInicio =
            DateTime.UtcNow.AddYears(-2);

        context.PersonaSuministros.Add(new PersonaSuministro
        {
            PersonaId = historicalPerson.Id,
            SuministroId = setup.Supply.Id,
            FechaInicio = DateTime.UtcNow.AddYears(-1),
            FechaFin = DateTime.UtcNow.AddMonths(-6),
            Estado = EstadoRelacionSuministro.Finalizada
        });

        var obligation =
            SupplyObligation(setup.Supply.Id, 30m, "2026");

        context.Obligaciones.Add(obligation);
        await context.SaveChangesAsync();

        var service = new PagoService(context);

        var result = await service.RegisterAsync(
            new RegistrarPagoInput(
                30m,
                "Pago",
                new[] { obligation.Id }),
            setup.User.Id);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);

        Assert.Equal(
            setup.Person.Id,
            result.Value!.Titular.Id);

        Assert.Equal(
            "Juan Pérez",
            result.Value.Titular.Nombre);

        Assert.NotEqual(
            historicalPerson.Id,
            result.Value.Titular.Id);
    }
    [Fact]
    public async Task GetByIdAndComprobante_ReturnPersistedPaymentProjection()
    {
        await using var context = CreateContext();
        var setup = AddReferences(context);

        var obligation = SupplyObligation(setup.Supply.Id, 30m, "2026");
        context.Obligaciones.Add(obligation);
        await context.SaveChangesAsync();

        var service = new PagoService(context);

        var registered = await service.RegisterAsync(
            new RegistrarPagoInput(
                30m,
                "Pago",
                new[] { obligation.Id }),
            setup.User.Id);

        var detail = await service.GetByIdAsync(registered.Value!.Id);
        var receipt = await service.GetComprobanteAsync(registered.Value.Id);

        Assert.NotNull(detail);
        Assert.NotNull(receipt);
        Assert.Equal(detail!.NumeroComprobante, receipt!.Numero);
        Assert.Equal(detail.Monto, receipt.Total);
        Assert.Single(receipt.Obligaciones);
        Assert.Equal(setup.User.NombreUsuario, receipt.UsuarioAdministrativo);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsRegisteredPayments()
    {
        await using var context = CreateContext();
        var setup = AddReferences(context);

        var obligation = SupplyObligation(setup.Supply.Id, 30m, "2026");
        context.Obligaciones.Add(obligation);
        await context.SaveChangesAsync();

        var service = new PagoService(context);

        await service.RegisterAsync(
            new RegistrarPagoInput(
                30m,
                "Pago",
                new[] { obligation.Id }),
            setup.User.Id);

        var payments = await service.GetAllAsync();

        var payment = Assert.Single(payments);
        Assert.Equal("PAG-000001", payment.NumeroComprobante);
        Assert.Equal(30m, payment.Monto);
    }

    [Fact]
    public async Task RegisterAsync_RejectsInvalidInputBeforePersistence()
    {
        await using var context = CreateContext();
        var setup = AddReferences(context);
        var service = new PagoService(context);

        var emptyConcept = await service.RegisterAsync(
            new RegistrarPagoInput(30m, " ", new[] { 1 }),
            setup.User.Id);

        var zeroAmount = await service.RegisterAsync(
            new RegistrarPagoInput(0m, "Pago", new[] { 1 }),
            setup.User.Id);

        var emptyObligations = await service.RegisterAsync(
            new RegistrarPagoInput(30m, "Pago", Array.Empty<int>()),
            setup.User.Id);

        Assert.Equal(PagoOperationError.Invalid, emptyConcept.Error);
        Assert.Equal(PagoOperationError.Invalid, zeroAmount.Error);
        Assert.Equal(PagoOperationError.Invalid, emptyObligations.Error);
        Assert.Empty(context.Pagos);
    }

    private static Obligacion SupplyObligation(
        int supplyId,
        decimal amount,
        string period) =>
        new()
        {
            PersonaId = null,
            SuministroId = supplyId,
            CuotaId = null,
            Origen = OrigenObligacion.CuotaOrdinaria,
            Concepto = "Cuota ordinaria",
            Monto = amount,
            Periodo = period,
            FechaGeneracion = DateTime.UtcNow,
            Estado = EstadoObligacion.Pendiente
        };

    private static (
        UsuarioAdministrativo User,
        Persona Person,
        Suministro Supply)
        AddReferences(PanyebarDbContext context)
    {
        var user = new UsuarioAdministrativo
        {
            NombreUsuario = "admin",
            PasswordHash = "hash",
            Estado = EstadoRegistro.Activo
        };

        var person = new Persona
        {
            Nombres = "Juan",
            Apellidos = "Pérez",
            Identificacion = "1234567890101",
            Estado = EstadoRegistro.Activo
        };

        var supply = new Suministro
        {
            SectorId = 1,
            Nis = "PAN-000001",
            CodigoQrToken = "token-1",
            DireccionReferencia = "Centro",
            Estado = EstadoSuministro.Activo
        };

        context.AddRange(user, person, supply);
        context.SaveChanges();

        context.PersonaSuministros.Add(new PersonaSuministro
        {
            PersonaId = person.Id,
            SuministroId = supply.Id,
            FechaInicio = DateTime.UtcNow.AddYears(-1),
            Estado = EstadoRelacionSuministro.Vigente
        });

        context.SaveChanges();

        return (user, person, supply);
    }

    private static PanyebarDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<PanyebarDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        return new PanyebarDbContext(options);
    }
}