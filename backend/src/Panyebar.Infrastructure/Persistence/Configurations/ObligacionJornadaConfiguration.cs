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
            // Una participación puede originar como máximo una obligación.
            builder
                .HasOne(oj => oj.ParticipacionJornada)
                .WithOne()
                .HasForeignKey<ObligacionJornada>(oj => oj.ParticipacionJornadaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Una obligación puede corresponder como máximo a una participación.
            builder
                .HasOne(oj => oj.Obligacion)
                .WithOne()
                .HasForeignKey<ObligacionJornada>(oj => oj.ObligacionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(oj => oj.ParticipacionJornadaId)
                .IsUnique()
                .HasDatabaseName("IX_ObligacionesJornada_ParticipacionJornadaId");

            builder.HasIndex(oj => oj.ObligacionId)
                .IsUnique()
                .HasDatabaseName("IX_ObligacionesJornada_ObligacionId");
        }
    }
}
