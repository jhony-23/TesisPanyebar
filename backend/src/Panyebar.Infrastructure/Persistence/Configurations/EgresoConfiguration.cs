using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de relaciones e índices para la entidad Egreso.
    /// </summary>
    public class EgresoConfiguration : IEntityTypeConfiguration<Egreso>
    {
        public void Configure(EntityTypeBuilder<Egreso> builder)
        {
            // Relación 14: UsuarioAdministrativo → Egreso (1 : N, unidireccional)
            // Se configura desde UsuarioAdministrativoConfiguration

            // Precisión monetaria
            builder.Property(e => e.Monto)
                .HasPrecision(18, 2);
        }
    }
}
