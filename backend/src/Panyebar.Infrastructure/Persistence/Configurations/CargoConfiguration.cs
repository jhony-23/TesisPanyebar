using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de relaciones para la entidad Cargo.
    /// </summary>
    public class CargoConfiguration : IEntityTypeConfiguration<Cargo>
    {
        public void Configure(EntityTypeBuilder<Cargo> builder)
        {
            // Relación 21: Cargo → IntegranteAdministracion (1 : N)
            builder
                .HasMany<IntegranteAdministracion>()
                .WithOne(ia => ia.Cargo)
                .HasForeignKey(ia => ia.CargoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
