using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Panyebar.Application.Security;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;
using Panyebar.Infrastructure.Persistence;
using Xunit;

namespace Panyebar.Security.Tests;

public class JornadasModelPersistenceTests
{
    private static PanyebarDbContext CreateSqlServerModelContext()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseSqlServer("Server=dummy;Database=dummy;Trusted_Connection=True;")
            .Options;

        return new PanyebarDbContext(options);
    }

    [Fact]
    public void JornadaEnums_PreserveApprovedValues()
    {
        Assert.Equal(1, (int)EstadoJornada.Planificada);
        Assert.Equal(2, (int)EstadoJornada.Cerrada);
        Assert.Equal(3, (int)EstadoJornada.Cancelada);

        Assert.Equal(0, (int)ResultadoParticipacionJornada.Pendiente);
        Assert.Equal(1, (int)ResultadoParticipacionJornada.Participacion);
        Assert.Equal(2, (int)ResultadoParticipacionJornada.Ausencia);
        Assert.Equal(3, (int)ResultadoParticipacionJornada.AusenciaJustificada);
        Assert.Equal(2, (int)OrigenObligacion.Jornada);
    }

    [Fact]
    public void JornadaAndParticipation_SupportApprovedShapeAndDefaults()
    {
        var jornada = new Jornada
        {
            Nombre = "Limpieza del nacimiento",
            Descripcion = null,
            Fecha = new DateTime(2026, 10, 15),
            HoraInicio = new TimeOnly(8, 0),
            HoraFin = new TimeOnly(12, 0),
            Ubicacion = "Nacimiento comunal",
            MontoIncumplimiento = 50.00m
        };
        var participacion = new ParticipacionJornada();

        Assert.Equal(EstadoJornada.Planificada, jornada.Estado);
        Assert.Equal(new TimeOnly(8, 0), jornada.HoraInicio);
        Assert.Equal(new TimeOnly(12, 0), jornada.HoraFin);
        Assert.Equal(50.00m, jornada.MontoIncumplimiento);
        Assert.Equal(ResultadoParticipacionJornada.Pendiente, participacion.Resultado);
    }

    [Fact]
    public void EfModel_JornadaConfiguration_HasApprovedPropertiesAndDefaults()
    {
        using var context = CreateSqlServerModelContext();
        var entity = context.Model.FindEntityType(typeof(Jornada));
        Assert.NotNull(entity);

        AssertProperty(entity, nameof(Jornada.Nombre), nullable: false, maxLength: 150);
        AssertProperty(entity, nameof(Jornada.Descripcion), nullable: true, maxLength: 500);
        AssertProperty(entity, nameof(Jornada.Ubicacion), nullable: true, maxLength: 200);

        var amount = entity.FindProperty(nameof(Jornada.MontoIncumplimiento));
        Assert.NotNull(amount);
        Assert.True(amount.IsNullable);
        Assert.Equal(18, amount.GetPrecision());
        Assert.Equal(2, amount.GetScale());

        Assert.Equal("time", entity.FindProperty(nameof(Jornada.HoraInicio))?.GetColumnType());
        Assert.Equal("time", entity.FindProperty(nameof(Jornada.HoraFin))?.GetColumnType());
    }

    [Fact]
    public void EfDesignModel_Jornada_HasAmountScheduleAndStateChecks()
    {
        using var context = CreateSqlServerModelContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType(typeof(Jornada));
        Assert.NotNull(entity);

        AssertCheck(entity, "CK_Jornadas_MontoIncumplimientoPositivo", "[MontoIncumplimiento] IS NULL OR [MontoIncumplimiento] > 0");
        AssertCheck(entity, "CK_Jornadas_HorarioValido", "[HoraInicio] IS NULL OR [HoraFin] IS NULL OR [HoraFin] > [HoraInicio]");
        AssertCheck(entity, "CK_Jornadas_EstadoValido", "[Estado] IN (1, 2, 3)");
    }

    [Fact]
    public void EfModel_Participation_HasDefaultsLengthUniqueParticipantAndResultCheck()
    {
        using var context = CreateSqlServerModelContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ParticipacionJornada));
        Assert.NotNull(entity);

        AssertProperty(entity, nameof(ParticipacionJornada.Observacion), nullable: true, maxLength: 500);
        var participantIndex = entity.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(ParticipacionJornada.JornadaId), nameof(ParticipacionJornada.PersonaId) }));
        Assert.True(participantIndex.IsUnique);

        AssertCheck(entity, "CK_ParticipacionesJornada_ResultadoValido", "[Resultado] IN (0, 1, 2, 3)");
    }

    [Fact]
    public void EfModel_ObligacionJornada_HasIndependentUniqueRestrictedRelationships()
    {
        using var context = CreateSqlServerModelContext();
        var entity = context.Model.FindEntityType(typeof(ObligacionJornada));
        Assert.NotNull(entity);

        var participationIndex = entity.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(ObligacionJornada.ParticipacionJornadaId) }));
        Assert.True(participationIndex.IsUnique);
        Assert.Equal("IX_ObligacionesJornada_ParticipacionJornadaId", participationIndex.GetDatabaseName());

        var obligationIndex = entity.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(ObligacionJornada.ObligacionId) }));
        Assert.True(obligationIndex.IsUnique);
        Assert.Equal("IX_ObligacionesJornada_ObligacionId", obligationIndex.GetDatabaseName());

        Assert.All(entity.GetForeignKeys(), foreignKey => Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
    }

    [Fact]
    public void EfDesignModel_Obligation_PreservesTitularXorCheck()
    {
        using var context = CreateSqlServerModelContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Obligacion));
        Assert.NotNull(entity);

        var constraint = entity.GetCheckConstraints().Single(check => check.Name == "CK_Obligaciones_TitularXor");
        Assert.Contains("[SuministroId] IS NOT NULL", constraint.Sql);
        Assert.Contains("[PersonaId] IS NULL", constraint.Sql);
    }

    [Fact]
    public void AdministrativePermissionCodes_ContainsJornadaPermissions()
    {
        Assert.Equal("JORNADAS.VER", AdministrativePermissionCodes.JornadasVer);
        Assert.Equal("JORNADAS.GESTIONAR", AdministrativePermissionCodes.JornadasGestionar);
    }

    private static void AssertProperty(
        IReadOnlyEntityType entity,
        string propertyName,
        bool nullable,
        int maxLength)
    {
        var property = entity.FindProperty(propertyName);
        Assert.NotNull(property);
        Assert.Equal(nullable, property.IsNullable);
        Assert.Equal(maxLength, property.GetMaxLength());
    }

    private static void AssertCheck(IReadOnlyEntityType entity, string name, string sql)
    {
        var constraint = entity.GetCheckConstraints().Single(check => check.Name == name);
        Assert.Equal(sql, constraint.Sql);
    }
}
