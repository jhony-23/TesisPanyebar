using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de relaciones para la entidad UsuarioAdministrativo.
    /// </summary>
    public class UsuarioAdministrativoConfiguration : IEntityTypeConfiguration<UsuarioAdministrativo>
    {
        public void Configure(EntityTypeBuilder<UsuarioAdministrativo> builder)
        {
            // Relación 13: UsuarioAdministrativo → Pago (1 : N, unidireccional)
            builder
                .HasMany<Pago>()
                .WithOne()
                .HasForeignKey(p => p.UsuarioAdministrativoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación 14: UsuarioAdministrativo → Egreso (1 : N, unidireccional)
            builder
                .HasMany<Egreso>()
                .WithOne()
                .HasForeignKey(e => e.UsuarioAdministrativoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación 15: UsuarioAdministrativo → UsuarioRol (1 : N)
            builder
                .HasMany<UsuarioRol>()
                .WithOne(ur => ur.UsuarioAdministrativo)
                .HasForeignKey(ur => ur.UsuarioAdministrativoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación 22: UsuarioAdministrativo → Auditoria (1 : N)
            builder
                .HasMany<Auditoria>()
                .WithOne(a => a.UsuarioAdministrativo)
                .HasForeignKey(a => a.UsuarioAdministrativoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Restricción 10: NombreUsuario único
            builder.HasIndex(ua => ua.NombreUsuario)
                .IsUnique();
        }
    }
}
