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
            // Relación 6: Cuota → Obligacion (1 : N, optional)
            builder
                .HasMany<Obligacion>()
                .WithOne(o => o.Cuota)
                .HasForeignKey(o => o.CuotaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
