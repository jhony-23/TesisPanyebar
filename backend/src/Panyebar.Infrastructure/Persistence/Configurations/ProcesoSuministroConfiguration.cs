using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    public class ProcesoSuministroConfiguration : IEntityTypeConfiguration<ProcesoSuministro>
    {
        public void Configure(EntityTypeBuilder<ProcesoSuministro> builder)
        {
            builder.Property(p => p.Motivo)
                .IsRequired()
                .HasMaxLength(250);

            builder.Property(p => p.Observacion)
                .HasMaxLength(1000);

            builder
                .HasOne(p => p.Suministro)
                .WithMany()
                .HasForeignKey(p => p.SuministroId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne(p => p.UsuarioAdministrativo)
                .WithMany()
                .HasForeignKey(p => p.UsuarioAdministrativoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => new { p.SuministroId, p.Fecha });
        }
    }
}