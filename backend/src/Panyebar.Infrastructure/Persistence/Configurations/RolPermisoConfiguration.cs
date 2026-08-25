using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de relaciones e índices para la entidad RolPermiso.
    /// </summary>
    public class RolPermisoConfiguration : IEntityTypeConfiguration<RolPermiso>
    {
        public void Configure(EntityTypeBuilder<RolPermiso> builder)
        {
            // Relación 17: Rol → RolPermiso (1 : N)
            // Se configura desde RolConfiguration

            // Relación 18: Permiso → RolPermiso (1 : N)
            // Se configura desde PermisoConfiguration

            // Restricción 8: Un rol no puede tener el mismo permiso dos veces
            builder.HasIndex(rp => new { rp.RolId, rp.PermisoId })
                .IsUnique();
        }
    }
}
