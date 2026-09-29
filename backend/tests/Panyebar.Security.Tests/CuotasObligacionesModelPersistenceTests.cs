using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Panyebar.Application.Security;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;
using Xunit;

namespace Panyebar.Security.Tests;

public class CuotasObligacionesModelPersistenceTests
{
    [Fact]
    public void AdministrativeOrigin_UsesIntegerStorageWithoutRestrictiveOriginCheck()
    {
        using var context = CreateSqlServerModelContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Obligacion))!;
        Assert.Equal("int", entity.FindProperty(nameof(Obligacion.Origen))!.GetColumnType());
        Assert.DoesNotContain(entity.GetCheckConstraints(), c => c.Sql.Contains("Origen", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(entity.GetCheckConstraints(), c => c.Name == "CK_Obligaciones_TitularXor");
        var sql = context.Obligaciones.Where(o => o.Origen == OrigenObligacion.Administrativa).ToQueryString();
        Assert.Contains("[Origen] = 3", sql);
    }

    private static PanyebarDbContext CreateSqlServerModelContext()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseSqlServer("Server=dummy;Database=dummy;Trusted_Connection=True;")
            .Options;

        return new PanyebarDbContext(options);
    }

    [Fact]
    public void PeriodicidadCuota_ContainsExpectedValues()
    {
        Assert.Equal(1, (int)PeriodicidadCuota.Anual);
        Assert.Equal(2, (int)PeriodicidadCuota.Mensual);
        Assert.True(Enum.IsDefined(typeof(PeriodicidadCuota), PeriodicidadCuota.Anual));
        Assert.True(Enum.IsDefined(typeof(PeriodicidadCuota), PeriodicidadCuota.Mensual));
    }

    [Fact]
    public void EstadoObligacion_PreservesExistingValuesAndAddsAnulada()
    {
        Assert.Equal(1, (int)EstadoObligacion.Pendiente);
        Assert.Equal(2, (int)EstadoObligacion.Pagada);
        Assert.Equal(3, (int)EstadoObligacion.Anulada);
        Assert.True(Enum.IsDefined(typeof(EstadoObligacion), EstadoObligacion.Pendiente));
        Assert.True(Enum.IsDefined(typeof(EstadoObligacion), EstadoObligacion.Pagada));
        Assert.True(Enum.IsDefined(typeof(EstadoObligacion), EstadoObligacion.Anulada));
    }

    [Fact]
    public void OrigenObligacion_PreservesExpectedValues()
    {
        Assert.Equal(1, (int)OrigenObligacion.CuotaOrdinaria);
        Assert.Equal(2, (int)OrigenObligacion.Jornada);
        Assert.Equal(3, (int)OrigenObligacion.Administrativa);
    }

    [Fact]
    public void Cuota_SupportsNombreAndDescripcion_AndDomainDefaults()
    {
        var cuota = new Cuota
        {
            Id = 1,
            Nombre = "Cuota Ordinaria 2026",
            Descripcion = "Cuota comunitaria anual",
            Monto = 30.00m,
            Periodicidad = PeriodicidadCuota.Anual,
            FechaInicioVigencia = new DateTime(2026, 1, 1),
            FechaFinVigencia = null,
            Estado = EstadoRegistro.Activo
        };

        Assert.Equal("Cuota Ordinaria 2026", cuota.Nombre);
        Assert.Equal("Cuota comunitaria anual", cuota.Descripcion);
        Assert.Equal(30.00m, cuota.Monto);
        Assert.Equal(PeriodicidadCuota.Anual, cuota.Periodicidad);
        Assert.Equal(EstadoRegistro.Activo, cuota.Estado);
    }

    [Fact]
    public void Obligacion_SupportsCuotaIdAndTitularPersonaOrSuministro()
    {
        var obligacionSuministro = new Obligacion
        {
            Id = 10,
            SuministroId = 5,
            PersonaId = null,
            CuotaId = 1,
            Origen = OrigenObligacion.CuotaOrdinaria,
            Concepto = "Cuota Ordinaria 2026",
            Monto = 30.00m,
            Periodo = "2026",
            FechaGeneracion = new DateTime(2026, 1, 15),
            FechaVencimiento = new DateTime(2026, 3, 31),
            Estado = EstadoObligacion.Pendiente
        };

        var obligacionPersona = new Obligacion
        {
            Id = 11,
            SuministroId = null,
            PersonaId = 8,
            CuotaId = null,
            Origen = OrigenObligacion.Jornada,
            Concepto = "Inasistencia Jornada 15/02/2026",
            Monto = 50.00m,
            Periodo = null,
            FechaGeneracion = new DateTime(2026, 2, 16),
            FechaVencimiento = null,
            Estado = EstadoObligacion.Pendiente
        };

        Assert.NotNull(obligacionSuministro.SuministroId);
        Assert.Null(obligacionSuministro.PersonaId);
        Assert.Equal(1, obligacionSuministro.CuotaId);
        Assert.Equal("2026", obligacionSuministro.Periodo);
        Assert.NotNull(obligacionSuministro.FechaVencimiento);

        Assert.Null(obligacionPersona.SuministroId);
        Assert.NotNull(obligacionPersona.PersonaId);
        Assert.Null(obligacionPersona.CuotaId);
        Assert.Null(obligacionPersona.Periodo);
        Assert.Null(obligacionPersona.FechaVencimiento);
    }

    [Fact]
    public void AdministrativePermissionCodes_ContainsExpectedCodes()
    {
        Assert.Equal("CUOTAS.VER", AdministrativePermissionCodes.CuotasVer);
        Assert.Equal("CUOTAS.GESTIONAR", AdministrativePermissionCodes.CuotasGestionar);
        Assert.Equal("OBLIGACIONES.VER", AdministrativePermissionCodes.ObligacionesVer);
        Assert.Equal("OBLIGACIONES.GESTIONAR", AdministrativePermissionCodes.ObligacionesGestionar);
    }

    [Fact]
    public void EfModel_CuotaConfiguration_HasExpectedLengthsAndMonetaryPrecision()
    {
        using var context = CreateSqlServerModelContext();
        var entity = context.Model.FindEntityType(typeof(Cuota));
        Assert.NotNull(entity);

        var propNombre = entity.FindProperty(nameof(Cuota.Nombre));
        Assert.NotNull(propNombre);
        Assert.False(propNombre.IsNullable);
        Assert.Equal(100, propNombre.GetMaxLength());

        var propDescripcion = entity.FindProperty(nameof(Cuota.Descripcion));
        Assert.NotNull(propDescripcion);
        Assert.True(propDescripcion.IsNullable);
        Assert.Equal(500, propDescripcion.GetMaxLength());

        var propMonto = entity.FindProperty(nameof(Cuota.Monto));
        Assert.NotNull(propMonto);
        Assert.Equal(18, propMonto.GetPrecision());
        Assert.Equal(2, propMonto.GetScale());
    }

    [Fact]
    public void EfModel_ObligacionConfiguration_HasExpectedLengthsAndMonetaryPrecision()
    {
        using var context = CreateSqlServerModelContext();
        var entity = context.Model.FindEntityType(typeof(Obligacion));
        Assert.NotNull(entity);

        var propConcepto = entity.FindProperty(nameof(Obligacion.Concepto));
        Assert.NotNull(propConcepto);
        Assert.False(propConcepto.IsNullable);
        Assert.Equal(200, propConcepto.GetMaxLength());

        var propPeriodo = entity.FindProperty(nameof(Obligacion.Periodo));
        Assert.NotNull(propPeriodo);
        Assert.True(propPeriodo.IsNullable);
        Assert.Equal(20, propPeriodo.GetMaxLength());

        var propFechaVencimiento = entity.FindProperty(nameof(Obligacion.FechaVencimiento));
        Assert.NotNull(propFechaVencimiento);
        Assert.True(propFechaVencimiento.IsNullable);

        var propMonto = entity.FindProperty(nameof(Obligacion.Monto));
        Assert.NotNull(propMonto);
        Assert.Equal(18, propMonto.GetPrecision());
        Assert.Equal(2, propMonto.GetScale());
    }

    // NOTE: Check constraint CK_Obligaciones_TitularXor is verified in the migration.
    // SQL Server runtime models do not expose check constraints reliably in EF Core 8.
    // The constraint is defined in ObligacionConfiguration and will be validated when
    // the database migration is applied.

    [Fact]
    public void EfModel_ObligacionConfiguration_ContainsFilteredUniqueIndexForCuotaObligations()
    {
        using var context = CreateSqlServerModelContext();
        var entity = context.Model.FindEntityType(typeof(Obligacion));
        Assert.NotNull(entity);

        var index = entity.GetIndexes().FirstOrDefault(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(Obligacion.CuotaId), nameof(Obligacion.SuministroId), nameof(Obligacion.Periodo) }));

        Assert.NotNull(index);
        Assert.True(index.IsUnique);
        Assert.Equal("IX_Obligaciones_CuotaId_SuministroId_Periodo", index.GetDatabaseName());

        var filter = index.GetFilter();
        Assert.NotNull(filter);
        Assert.Contains("[CuotaId] IS NOT NULL", filter);
        Assert.Contains("[SuministroId] IS NOT NULL", filter);
        Assert.Contains("[Periodo] IS NOT NULL", filter);
        Assert.Contains("[Estado] <> 3", filter);
    }

    [Fact]
    public void EfModel_ObligacionConfiguration_ContainsOptimizationIndexesForTitularAndEstado()
    {
        using var context = CreateSqlServerModelContext();
        var entity = context.Model.FindEntityType(typeof(Obligacion));
        Assert.NotNull(entity);

        var suministroIndex = entity.GetIndexes().FirstOrDefault(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(Obligacion.SuministroId), nameof(Obligacion.Estado) }));
        Assert.NotNull(suministroIndex);
        Assert.Equal("IX_Obligaciones_SuministroId_Estado", suministroIndex.GetDatabaseName());

        var personaIndex = entity.GetIndexes().FirstOrDefault(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(Obligacion.PersonaId), nameof(Obligacion.Estado) }));
        Assert.NotNull(personaIndex);
        Assert.Equal("IX_Obligaciones_PersonaId_Estado", personaIndex.GetDatabaseName());
    }
}
