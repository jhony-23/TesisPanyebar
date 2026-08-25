using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de relaciones para la entidad Permiso.
    /// </summary>
    public class PermisoConfiguration : IEntityTypeConfiguration<Permiso>
    {
        public void Configure(EntityTypeBuilder<Permiso> builder)
        {
            // Relación 18: Permiso → RolPermiso (1 : N)
            builder
                .HasMany<RolPermiso>()
                .WithOne(rp => rp.Permiso)
                .HasForeignKey(rp => rp.PermisoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
