using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de relaciones para la entidad ParticipacionJornada.
    /// </summary>
    public class ParticipacionJornadaConfiguration : IEntityTypeConfiguration<ParticipacionJornada>
    {
        public void Configure(EntityTypeBuilder<ParticipacionJornada> builder)
        {
            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_ParticipacionesJornada_ResultadoValido",
                    "[Resultado] IN (0, 1, 2, 3)");
            });

            builder.Property(pj => pj.Observacion)
                .HasMaxLength(500)
                .IsRequired(false);

            // Restricción 4: Una sola participación por jornada y persona
            builder.HasIndex(pj => new { pj.JornadaId, pj.PersonaId })
                .IsUnique();
        }
    }
}
