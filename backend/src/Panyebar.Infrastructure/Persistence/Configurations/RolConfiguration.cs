using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de relaciones para la entidad Rol.
    /// </summary>
    public class RolConfiguration : IEntityTypeConfiguration<Rol>
    {
        public void Configure(EntityTypeBuilder<Rol> builder)
        {
            // Relación 16: Rol → UsuarioRol (1 : N)
            builder
                .HasMany<UsuarioRol>()
                .WithOne(ur => ur.Rol)
                .HasForeignKey(ur => ur.RolId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación 17: Rol → RolPermiso (1 : N)
            builder
                .HasMany<RolPermiso>()
                .WithOne(rp => rp.Rol)
                .HasForeignKey(rp => rp.RolId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
