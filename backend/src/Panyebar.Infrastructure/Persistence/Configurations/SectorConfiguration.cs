using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de relaciones para la entidad Sector.
    /// </summary>
    public class SectorConfiguration : IEntityTypeConfiguration<Sector>
    {
        public void Configure(EntityTypeBuilder<Sector> builder)
        {
            // Relación 1: Sector → Suministro (1 : N)
            builder
                .HasMany<Suministro>()
                .WithOne(s => s.Sector)
                .HasForeignKey(s => s.SectorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación 23: Sector → ProgramacionAbastecimiento (1 : N)
            builder
                .HasMany<ProgramacionAbastecimiento>()
                .WithOne(pa => pa.Sector)
                .HasForeignKey(pa => pa.SectorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
