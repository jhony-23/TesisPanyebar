using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de relaciones e índices para la entidad ObligacionJornada.
    /// </summary>
    public class ObligacionJornadaConfiguration : IEntityTypeConfiguration<ObligacionJornada>
    {
        public void Configure(EntityTypeBuilder<ObligacionJornada> builder)
        {
            // Relación 9: ParticipacionJornada → ObligacionJornada (1 : N)
            // Se configura desde ParticipacionJornadaConfiguration

            // Relación 10: Obligacion → ObligacionJornada (1 : N)
            // Se configura desde ObligacionConfiguration

            // Restricción 5: Una sola asociación entre participación y obligación
            builder.HasIndex(oj => new { oj.ParticipacionJornadaId, oj.ObligacionId })
                .IsUnique();
        }
    }
}
