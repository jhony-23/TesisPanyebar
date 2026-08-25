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
            // Relación 7: Jornada → ParticipacionJornada (1 : N)
            builder
                .HasMany<ParticipacionJornada>()
                .WithOne(pj => pj.Jornada)
                .HasForeignKey(pj => pj.JornadaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
