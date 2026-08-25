using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de relaciones para la entidad Pago.
    /// </summary>
    public class PagoConfiguration : IEntityTypeConfiguration<Pago>
    {
        public void Configure(EntityTypeBuilder<Pago> builder)
        {
            // Relación 11: Pago → AplicacionPago (1 : N)
            builder
                .HasMany<AplicacionPago>()
                .WithOne(ap => ap.Pago)
                .HasForeignKey(ap => ap.PagoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación 13: UsuarioAdministrativo → Pago (unidireccional, 1 : N)
            // Se configura desde UsuarioAdministrativoConfiguration
        }
    }
}
