using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Panyebar.Application.Security;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;
using Panyebar.Infrastructure.Persistence.Migrations;

namespace Panyebar.Security.Tests;

public sealed class FinancialManagementModelPersistenceTests
{
    [Fact]
    public void Egreso_HasOnlyApprovedFieldsAndDefaultsToRegistrado()
    {
        Assert.Equal(EstadoEgreso.Registrado, new Egreso().Estado);
        Assert.Equal(
            new[] { "Concepto", "Estado", "Fecha", "Id", "Monto", "UsuarioAdministrativoId" },
            typeof(Egreso).GetProperties().Select(p => p.Name).OrderBy(name => name));
    }

    [Fact]
    public void EstadoEgreso_ContainsOnlyApprovedStates()
    {
        Assert.Equal(new[] { "Registrado", "Anulado" }, Enum.GetNames<EstadoEgreso>());
        Assert.Equal(new[] { 1, 2 }, Enum.GetValues<EstadoEgreso>().Select(value => (int)value));
    }

    [Fact]
    public void EfModel_Egreso_ProtectsAmountConceptAndState()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Egreso))!;
        var amount = entity.FindProperty(nameof(Egreso.Monto))!;
        Assert.Equal(18, amount.GetPrecision());
        Assert.Equal(2, amount.GetScale());
        Assert.Equal("decimal(18,2)", amount.GetColumnType());
        var concept = entity.FindProperty(nameof(Egreso.Concepto))!;
        Assert.False(concept.IsNullable);
        Assert.Equal(200, concept.GetMaxLength());
        var state = entity.FindProperty(nameof(Egreso.Estado))!;
        Assert.False(state.IsNullable);
        Assert.Equal("int", state.GetColumnType());
        Assert.Equal("[Monto] > 0", Assert.Single(entity.GetCheckConstraints(),
            c => c.Name == "CK_Egresos_MontoPositivo").Sql);
        Assert.Equal("[Estado] IN (1, 2)", Assert.Single(entity.GetCheckConstraints(),
            c => c.Name == "CK_Egresos_EstadoValido").Sql);
    }

    [Fact]
    public void EfModel_Egreso_PreservesSingleRequiredAdministrativeUserRelationship()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(Egreso))!;
        var foreignKey = Assert.Single(entity.GetForeignKeys());
        Assert.Equal(typeof(UsuarioAdministrativo), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(nameof(Egreso.UsuarioAdministrativoId), Assert.Single(foreignKey.Properties).Name);
        Assert.True(foreignKey.IsRequired);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void EfModel_EgresoFecha_RemainsCivilWhileAuditFechaRemainsUtc()
    {
        using var context = CreateContext();
        var date = context.Model.FindEntityType(typeof(Egreso))!.FindProperty(nameof(Egreso.Fecha))!;
        Assert.False(date.IsNullable);
        Assert.Equal(typeof(DateTime), date.ClrType);
        Assert.Equal("datetime2", date.GetColumnType());
        Assert.Null(date.GetValueConverter());
        Assert.Null(date.GetTypeMapping().Converter);
        Assert.NotNull(context.Model.FindEntityType(typeof(Auditoria))!
            .FindProperty(nameof(Auditoria.Fecha))!.GetValueConverter());
    }

    [Fact]
    public void AdministrativePermissionCodes_SeparatesFinanceFromPayments()
    {
        Assert.Equal("FINANZAS.VER", AdministrativePermissionCodes.FinanzasVer);
        Assert.Equal("FINANZAS.GESTIONAR", AdministrativePermissionCodes.FinanzasGestionar);
        Assert.Equal("PAGOS.VER", AdministrativePermissionCodes.PagosVer);
        Assert.Equal("PAGOS.GESTIONAR", AdministrativePermissionCodes.PagosGestionar);
    }

    [Fact]
    public void EfModel_DoesNotPersistIncomeMovementsOrBalance()
    {
        using var context = CreateContext();
        var entities = context.Model.GetEntityTypes().ToList();
        Assert.DoesNotContain(entities, e =>
            e.ClrType.Name.Contains("Ingreso") || e.ClrType.Name.Contains("MovimientoFinanciero") ||
            e.ClrType.Name.Contains("Balance"));
        Assert.DoesNotContain(entities, e =>
            (e.GetTableName() ?? "").Contains("Ingreso") ||
            (e.GetTableName() ?? "").Contains("MovimientoFinanciero") ||
            (e.GetTableName() ?? "").Contains("Balance"));
        Assert.DoesNotContain(entities.SelectMany(e => e.GetProperties()), p =>
            p.Name.Contains("Balance") || p.Name.Contains("Saldo"));
        Assert.NotNull(context.Model.FindEntityType(typeof(Pago)));
    }

    [Fact]
    public void Migration_RegistersExistingExpensesWithoutRecreatingTablesOrChangingDates()
    {
        var operations = new ConfigureFinancialManagement().UpOperations;
        var state = Assert.Single(operations.OfType<AddColumnOperation>());
        Assert.Equal("Egresos", state.Table);
        Assert.Equal("Estado", state.Name);
        Assert.Equal(1, state.DefaultValue);
        Assert.False(state.IsNullable);
        Assert.DoesNotContain(operations, operation =>
            operation is CreateTableOperation or DropTableOperation or DropColumnOperation);
        var alteration = Assert.Single(operations.OfType<AlterColumnOperation>());
        Assert.Equal("Egresos", alteration.Table);
        Assert.Equal("Concepto", alteration.Name);
        Assert.Equal(200, alteration.MaxLength);
    }

    [Fact]
    public void Migration_SeedsFinancePermissionsIdempotentlyAndPreservesAssignedPermissionsOnRollback()
    {
        var migration = new ConfigureFinancialManagement();
        var seeds = migration.UpOperations.OfType<SqlOperation>().ToList();
        Assert.Equal(3, seeds.Count);
        Assert.Contains("IF NOT EXISTS", seeds[0].Sql);
        Assert.Contains("N'FINANZAS.VER'", seeds[0].Sql);
        Assert.Contains("IF NOT EXISTS", seeds[1].Sql);
        Assert.Contains("N'FINANZAS.GESTIONAR'", seeds[1].Sql);
        Assert.Contains("[UsuarioRoles]", seeds[2].Sql);
        Assert.Contains("N'demo.admin'", seeds[2].Sql);
        Assert.Contains("NOT EXISTS", seeds[2].Sql);
        Assert.DoesNotContain(seeds, seed => seed.Sql.Contains("PAGOS."));
        var rollback = Assert.Single(migration.DownOperations.OfType<SqlOperation>()).Sql;
        Assert.Contains("NOT EXISTS", rollback);
        Assert.Contains("[RolPermisos]", rollback);
        Assert.DoesNotContain(migration.DownOperations, operation => operation is DropTableOperation);
    }

    private static PanyebarDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseSqlServer("Server=dummy;Database=dummy;Trusted_Connection=True;")
            .Options;
        return new PanyebarDbContext(options);
    }
}
