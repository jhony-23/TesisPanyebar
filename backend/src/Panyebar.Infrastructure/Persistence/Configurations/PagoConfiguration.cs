using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de persistencia y relaciones para la entidad Pago.
    /// </summary>
    public class PagoConfiguration : IEntityTypeConfiguration<Pago>
    {
        public void Configure(EntityTypeBuilder<Pago> builder)
        {
            // Un pago puede cancelar una o varias obligaciones completas.
            builder
                .HasMany<AplicacionPago>()
                .WithOne(ap => ap.Pago)
                .HasForeignKey(ap => ap.PagoId)
                .OnDelete(DeleteBehavior.Restrict);

            // La relación UsuarioAdministrativo -> Pago se configura desde
            // UsuarioAdministrativoConfiguration.

            builder.Property(p => p.Monto)
                .HasPrecision(18, 2);

            builder.Property(p => p.Concepto)
                .IsRequired()
                .HasMaxLength(200);

            // Un pago válido siempre representa un monto monetario positivo.
            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Pagos_MontoPositivo",
                    "[Monto] > 0");
            });
        }
    }
}
