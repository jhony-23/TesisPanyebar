using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence;

internal static class UtcDateTimeModelBuilderExtensions
{
    private static readonly ValueConverter<DateTime, DateTime> UtcConverter = new(
        value => ToUtc(value),
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> NullableUtcConverter = new(
        value => value.HasValue ? ToUtc(value.Value) : value,
        value => value.HasValue
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : value);

    public static void ConfigureUtcDateTimes(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Auditoria>()
            .Property(entity => entity.Fecha)
            .HasConversion(UtcConverter);
        modelBuilder.Entity<Obligacion>()
            .Property(entity => entity.FechaGeneracion)
            .HasConversion(UtcConverter);
        modelBuilder.Entity<Pago>()
            .Property(entity => entity.Fecha)
            .HasConversion(UtcConverter);
        modelBuilder.Entity<PersonaSuministro>()
            .Property(entity => entity.FechaInicio)
            .HasConversion(UtcConverter);
        modelBuilder.Entity<PersonaSuministro>()
            .Property(entity => entity.FechaFin)
            .HasConversion(NullableUtcConverter);
        modelBuilder.Entity<ProcesoSuministro>()
            .Property(entity => entity.Fecha)
            .HasConversion(UtcConverter);
        modelBuilder.Entity<SolicitudNuevoServicio>()
            .Property(entity => entity.FechaSolicitud)
            .HasConversion(UtcConverter);
        modelBuilder.Entity<SolicitudNuevoServicio>()
            .Property(entity => entity.FechaResolucion)
            .HasConversion(NullableUtcConverter);
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
