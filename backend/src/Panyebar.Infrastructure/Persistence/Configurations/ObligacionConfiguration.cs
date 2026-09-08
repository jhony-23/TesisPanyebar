using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de relaciones para la entidad Obligacion.
    /// </summary>
    public class ObligacionConfiguration : IEntityTypeConfiguration<Obligacion>
    {
        public void Configure(EntityTypeBuilder<Obligacion> builder)
        {
            builder.ToTable(t =>
            {
                // Restricción XOR: la obligación pertenece a Suministro o a Persona, nunca a ambos ni a ninguno.
                t.HasCheckConstraint(
                    "CK_Obligaciones_TitularXor",
                    "([SuministroId] IS NOT NULL AND [PersonaId] IS NULL) OR ([SuministroId] IS NULL AND [PersonaId] IS NOT NULL)");
            });

            builder.Property(o => o.Concepto)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(o => o.Periodo)
                .HasMaxLength(20)
                .IsRequired(false);

            // Relación 10: Obligacion → ObligacionJornada (1 : N)
            builder
                .HasMany<ObligacionJornada>()
                .WithOne(oj => oj.Obligacion)
                .HasForeignKey(oj => oj.ObligacionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación 12: Obligacion → AplicacionPago (1 : N)
            builder
                .HasMany<AplicacionPago>()
                .WithOne(ap => ap.Obligacion)
                .HasForeignKey(ap => ap.ObligacionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Precisión monetaria
            builder.Property(o => o.Monto)
                .HasPrecision(18, 2);

            // Restricción de unicidad para obligaciones derivadas de cuota (excluye obligaciones anuladas)
            builder.HasIndex(o => new { o.CuotaId, o.SuministroId, o.Periodo })
                .IsUnique()
                .HasDatabaseName("IX_Obligaciones_CuotaId_SuministroId_Periodo")
                .HasFilter("[CuotaId] IS NOT NULL AND [SuministroId] IS NOT NULL AND [Periodo] IS NOT NULL AND [Estado] <> 3");

            // Índices de optimización de consulta por titular y estado
            builder.HasIndex(o => new { o.SuministroId, o.Estado })
                .HasDatabaseName("IX_Obligaciones_SuministroId_Estado");

            builder.HasIndex(o => new { o.PersonaId, o.Estado })
                .HasDatabaseName("IX_Obligaciones_PersonaId_Estado");
        }
    }
}
