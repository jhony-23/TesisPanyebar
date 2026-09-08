using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de relaciones para la entidad Cuota.
    /// </summary>
    public class CuotaConfiguration : IEntityTypeConfiguration<Cuota>
    {
        public void Configure(EntityTypeBuilder<Cuota> builder)
        {
            builder.Property(c => c.Nombre)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Descripcion)
                .HasMaxLength(500);

            // Relación 6: Cuota → Obligacion (1 : N, optional)
            builder
                .HasMany<Obligacion>()
                .WithOne(o => o.Cuota)
                .HasForeignKey(o => o.CuotaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Precisión monetaria
            builder.Property(c => c.Monto)
                .HasPrecision(18, 2);
        }
    }
}
