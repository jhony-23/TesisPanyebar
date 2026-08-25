using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de relaciones para la entidad AdministracionComite.
    /// </summary>
    public class AdministracionComiteConfiguration : IEntityTypeConfiguration<AdministracionComite>
    {
        public void Configure(EntityTypeBuilder<AdministracionComite> builder)
        {
            // Relación 19: AdministracionComite → IntegranteAdministracion (1 : N)
            builder
                .HasMany<IntegranteAdministracion>()
                .WithOne(ia => ia.AdministracionComite)
                .HasForeignKey(ia => ia.AdministracionComiteId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
