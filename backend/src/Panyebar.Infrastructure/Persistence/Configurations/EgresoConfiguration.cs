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

            builder.Property(e => e.Concepto)
                .IsRequired()
                .HasMaxLength(200);

            // Fecha civil: conserva DateTime/datetime2 sin converter UTC.
            builder.Property(e => e.Fecha)
                .IsRequired();

            // Los enums se persisten como int por convención, igual que Pago.
            builder.Property(e => e.Estado)
                .IsRequired();

            builder.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Egresos_MontoPositivo", "[Monto] > 0");
                t.HasCheckConstraint("CK_Egresos_EstadoValido", "[Estado] IN (1, 2)");
            });
        }
    }
}
