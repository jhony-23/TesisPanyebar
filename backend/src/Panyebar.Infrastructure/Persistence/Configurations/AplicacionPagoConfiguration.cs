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

            // Una obligación puede conservar aplicaciones históricas de pagos
            // anulados y posteriormente asociarse a un nuevo pago válido.
            // La unicidad funcional se controla contra pagos registrados.

            // Se conserva además la unicidad explícita de la asociación.
            builder.HasIndex(ap => new { ap.PagoId, ap.ObligacionId })
                .IsUnique()
                .HasDatabaseName("IX_AplicacionesPago_PagoId_ObligacionId");
        }
    }
}
