using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de relaciones e índices para la entidad UsuarioRol.
    /// </summary>
    public class UsuarioRolConfiguration : IEntityTypeConfiguration<UsuarioRol>
    {
        public void Configure(EntityTypeBuilder<UsuarioRol> builder)
        {
            // Relación 15: UsuarioAdministrativo → UsuarioRol (1 : N)
            // Se configura desde UsuarioAdministrativoConfiguration

            // Relación 16: Rol → UsuarioRol (1 : N)
            // Se configura desde RolConfiguration

            // Restricción 7: Un usuario no puede tener el mismo rol dos veces
            builder.HasIndex(ur => new { ur.UsuarioAdministrativoId, ur.RolId })
                .IsUnique();
        }
    }
}
