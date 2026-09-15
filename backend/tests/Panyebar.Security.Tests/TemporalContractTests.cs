using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Panyebar.Application.Security;
using Panyebar.Domain.Entities;
using Panyebar.Infrastructure.Persistence;

namespace Panyebar.Security.Tests;

public sealed class TemporalContractTests
{
    [Fact]
    public void EfModel_UtcInstant_RestoresUtcKindAfterMaterialization()
    {
        using var context = CreateContext();
        var property = context.Model
            .FindEntityType(typeof(Pago))!
            .FindProperty(nameof(Pago.Fecha))!;
        var converter = property.GetValueConverter();
        var databaseValue = new DateTime(2026, 9, 15, 23, 37, 13, DateTimeKind.Unspecified);

        Assert.NotNull(converter);

        var materialized = Assert.IsType<DateTime>(
            converter!.ConvertFromProvider(databaseValue));

        Assert.Equal(databaseValue.Ticks, materialized.Ticks);
        Assert.Equal(DateTimeKind.Utc, materialized.Kind);
    }

    [Fact]
    public void JsonSerialization_UtcInstant_IncludesUtcDesignator()
    {
        var instant = new DateTime(2026, 9, 15, 23, 37, 13, DateTimeKind.Utc);

        var json = JsonSerializer.Serialize(new { Fecha = instant });

        Assert.Contains("2026-09-15T23:37:13Z", json);
    }

    [Fact]
    public void SecurityDateTimeOffset_PreservesUtcInstant()
    {
        var expiresAtUtc = new DateTimeOffset(2026, 9, 16, 0, 37, 13, TimeSpan.Zero);
        var token = new AccessTokenResult { ExpiresAtUtc = expiresAtUtc };

        var json = JsonSerializer.Serialize(token);
        var restored = JsonSerializer.Deserialize<AccessTokenResult>(json);

        Assert.NotNull(restored);
        Assert.Equal(expiresAtUtc, restored!.ExpiresAtUtc);
        Assert.Equal(TimeSpan.Zero, restored.ExpiresAtUtc.Offset);
    }

    [Fact]
    public void EfModel_CivilDates_DoNotUseUtcConversion()
    {
        using var context = CreateContext();
        var jornadaDate = context.Model
            .FindEntityType(typeof(Jornada))!
            .FindProperty(nameof(Jornada.Fecha))!;
        var dueDate = context.Model
            .FindEntityType(typeof(Obligacion))!
            .FindProperty(nameof(Obligacion.FechaVencimiento))!;

        Assert.Null(jornadaDate.GetValueConverter());
        Assert.Null(dueDate.GetValueConverter());

        var civilDate = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Unspecified);
        Assert.Contains("2026-09-15T00:00:00", JsonSerializer.Serialize(civilDate));
    }

    private static PanyebarDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PanyebarDbContext>()
            .UseSqlServer("Server=dummy;Database=dummy;Trusted_Connection=True;")
            .Options;

        return new PanyebarDbContext(options);
    }
}
