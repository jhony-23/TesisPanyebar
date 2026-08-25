using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de relaciones para la entidad Obligacion.
    /// </summary>
    public class ObligacionConfiguration : IEntityTypeConfiguration<Obligacion>
    {
        public void Configure(EntityTypeBuilder<Obligacion> builder)
        {
            // Relación 10: Obligacion → ObligacionJornada (1 : N)
            builder
                .HasMany<ObligacionJornada>()
                .WithOne(oj => oj.Obligacion)
                .HasForeignKey(oj => oj.ObligacionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación 12: Obligacion → AplicacionPago (1 : N)
            builder
                .HasMany<AplicacionPago>()
                .WithOne(ap => ap.Obligacion)
                .HasForeignKey(ap => ap.ObligacionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Precisión monetaria
            builder.Property(o => o.Monto)
                .HasPrecision(18, 2);
        }
    }
}
