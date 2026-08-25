using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de relaciones e índices para la entidad AplicacionPago.
    /// </summary>
    public class AplicacionPagoConfiguration : IEntityTypeConfiguration<AplicacionPago>
    {
        public void Configure(EntityTypeBuilder<AplicacionPago> builder)
        {
            // Relación 11: Pago → AplicacionPago (1 : N)
            // Se configura desde PagoConfiguration

            // Relación 12: Obligacion → AplicacionPago (1 : N)
            // Se configura desde ObligacionConfiguration

            // Restricción 6: Una sola asociación entre pago y obligación
            builder.HasIndex(ap => new { ap.PagoId, ap.ObligacionId })
                .IsUnique();
        }
    }
}
