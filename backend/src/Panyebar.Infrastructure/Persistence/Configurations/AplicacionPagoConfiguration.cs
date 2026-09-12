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
            // Las relaciones con Pago y Obligacion se configuran desde
            // PagoConfiguration y ObligacionConfiguration, respectivamente.

            // Una obligación completa solo puede quedar asociada a un pago.
            // Esta protección de persistencia complementa la validación funcional
            // y evita una segunda aplicación incluso ante concurrencia.
            builder.HasIndex(ap => ap.ObligacionId)
                .IsUnique()
                .HasDatabaseName("IX_AplicacionesPago_ObligacionId");

            // Se conserva además la unicidad explícita de la asociación.
            builder.HasIndex(ap => new { ap.PagoId, ap.ObligacionId })
                .IsUnique()
                .HasDatabaseName("IX_AplicacionesPago_PagoId_ObligacionId");
        }
    }
}
