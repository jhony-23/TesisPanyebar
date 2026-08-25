using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de relaciones e índices para la entidad IntegranteAdministracion.
    /// </summary>
    public class IntegranteAdministracionConfiguration : IEntityTypeConfiguration<IntegranteAdministracion>
    {
        public void Configure(EntityTypeBuilder<IntegranteAdministracion> builder)
        {
            // Relación 19: AdministracionComite → IntegranteAdministracion (1 : N)
            // Se configura desde AdministracionComiteConfiguration

            // Relación 20: Persona → IntegranteAdministracion (1 : N)
            // Se configura desde PersonaConfiguration

            // Relación 21: Cargo → IntegranteAdministracion (1 : N)
            // Se configura desde CargoConfiguration

            // Restricción 9: No repetir exactamente la misma asociación
            builder.HasIndex(ia => new { ia.AdministracionComiteId, ia.PersonaId, ia.CargoId })
                .IsUnique();
        }
    }
}
