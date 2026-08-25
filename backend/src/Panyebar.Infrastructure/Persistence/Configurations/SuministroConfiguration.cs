using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de relaciones para la entidad Suministro.
    /// </summary>
    public class SuministroConfiguration : IEntityTypeConfiguration<Suministro>
    {
        public void Configure(EntityTypeBuilder<Suministro> builder)
        {
            // Relación 3: Suministro → PersonaSuministro (1 : N)
            builder
                .HasMany<PersonaSuministro>()
                .WithOne(ps => ps.Suministro)
                .HasForeignKey(ps => ps.SuministroId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación 5: Suministro → Obligacion (1 : N, optional)
            builder
                .HasMany<Obligacion>()
                .WithOne(o => o.Suministro)
                .HasForeignKey(o => o.SuministroId)
                .OnDelete(DeleteBehavior.Restrict);

            // Restricción 1: NIS con longitud máxima y unicidad
            builder.Property(s => s.Nis)
                .HasMaxLength(10);

            builder.HasIndex(s => s.Nis)
                .IsUnique();

            // Restricción 2: CodigoQrToken con unicidad
            builder.HasIndex(s => s.CodigoQrToken)
                .IsUnique();
        }
    }
}
