using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de relaciones para la entidad Jornada.
    /// </summary>
    public class JornadaConfiguration : IEntityTypeConfiguration<Jornada>
    {
        public void Configure(EntityTypeBuilder<Jornada> builder)
        {
            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_Jornadas_MontoIncumplimientoPositivo",
                    "[MontoIncumplimiento] IS NULL OR [MontoIncumplimiento] > 0");
                table.HasCheckConstraint(
                    "CK_Jornadas_HorarioValido",
                    "[HoraInicio] IS NULL OR [HoraFin] IS NULL OR [HoraFin] > [HoraInicio]");
                table.HasCheckConstraint(
                    "CK_Jornadas_EstadoValido",
                    "[Estado] IN (1, 2, 3)");
            });

            builder.Property(j => j.Nombre)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(j => j.Descripcion)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(j => j.HoraInicio)
                .HasColumnType("time");

            builder.Property(j => j.HoraFin)
                .HasColumnType("time");

            builder.Property(j => j.Ubicacion)
                .HasMaxLength(200)
                .IsRequired(false);

            // Relación 7: Jornada → ParticipacionJornada (1 : N)
            builder
                .HasMany<ParticipacionJornada>()
                .WithOne(pj => pj.Jornada)
                .HasForeignKey(pj => pj.JornadaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Precisión monetaria
            builder.Property(j => j.MontoIncumplimiento)
                .HasPrecision(18, 2);
        }
    }
}
