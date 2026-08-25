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
            // Relación 9: ParticipacionJornada → ObligacionJornada (1 : N)
            builder
                .HasMany<ObligacionJornada>()
                .WithOne(oj => oj.ParticipacionJornada)
                .HasForeignKey(oj => oj.ParticipacionJornadaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
