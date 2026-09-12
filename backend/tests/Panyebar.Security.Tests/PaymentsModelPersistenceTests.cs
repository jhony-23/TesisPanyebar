using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Panyebar.Application.Security;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;

namespace Panyebar.Security.Tests;

public sealed class PaymentsModelPersistenceTests
{
    [Fact]
    public void Pago_PreservesApprovedShapeAndDefaultState()
    {
        var pago = new Pago
        {
            Monto = 60.00m,
            Fecha = new DateTime(2026, 9, 12, 6, 0, 0, DateTimeKind.Utc),
            Concepto = "Pago de obligaciones",
            UsuarioAdministrativoId = 17
        };

        Assert.Equal(60.00m, pago.Monto);
        Assert.Equal("Pago de obligaciones", pago.Concepto);
        Assert.Equal(17, pago.UsuarioAdministrativoId);
        Assert.Equal(EstadoPago.Registrado, pago.Estado);
    }

    [Fact]
    public void EstadoPago_PreservesHistoricalValues()
    {
        Assert.Equal(1, (int)EstadoPago.Registrado);
        Assert.Equal(2, (int)EstadoPago.Anulado);
    }

    [Fact]
    public void AdministrativePermissionCodes_ContainsPaymentPermissions()
    {
        Assert.Equal("PAGOS.VER", AdministrativePermissionCodes.PagosVer);
        Assert.Equal("PAGOS.GESTIONAR", AdministrativePermissionCodes.PagosGestionar);
    }

    [Fact]
    public void EfModel_Pago_HasApprovedMonetaryPrecisionConceptLengthAndPositiveAmountCheck()
    {
        using var context = CreateContext();

        var designModel = context.GetService<IDesignTimeModel>().Model;
        var entity = designModel.FindEntityType(typeof(Pago));

        Assert.NotNull(entity);

        var amount = entity!.FindProperty(nameof(Pago.Monto));
        Assert.NotNull(amount);
        Assert.Equal(18, amount!.GetPrecision());
        Assert.Equal(2, amount.GetScale());

        var concept = entity.FindProperty(nameof(Pago.Concepto));
        Assert.NotNull(concept);
        Assert.False(concept!.IsNullable);
        Assert.Equal(200, concept.GetMaxLength());

        var table = entity.GetTableName();
        Assert.NotNull(table);

        var checkConstraints = entity.GetCheckConstraints().ToList();
        var amountCheck = Assert.Single(
            checkConstraints,
            constraint => constraint.Name == "CK_Pagos_MontoPositivo");

        Assert.Contains("[Monto] > 0", amountCheck.Sql);
    }

    [Fact]
    public void EfModel_AplicacionPago_ProtectsObligationFromMultiplePayments()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(AplicacionPago));

        Assert.NotNull(entity);

        var obligationIndex = Assert.Single(
            entity!.GetIndexes(),
            index =>
                index.Properties.Count == 1 &&
                index.Properties[0].Name == nameof(AplicacionPago.ObligacionId));

        Assert.True(obligationIndex.IsUnique);
        Assert.Equal("IX_AplicacionesPago_ObligacionId", obligationIndex.GetDatabaseName());

        var associationIndex = Assert.Single(
            entity.GetIndexes(),
            index =>
                index.Properties.Count == 2 &&
                index.Properties[0].Name == nameof(AplicacionPago.PagoId) &&
                index.Properties[1].Name == nameof(AplicacionPago.ObligacionId));

        Assert.True(associationIndex.IsUnique);
    }

    private static PanyebarDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseInMemoryDatabase($"payments-model-{Guid.NewGuid()}")
            .Options;

        return new PanyebarDbContext(options);
    }
}
