using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    public class SolicitudNuevoServicioConfiguration : IEntityTypeConfiguration<SolicitudNuevoServicio>
    {
        public void Configure(EntityTypeBuilder<SolicitudNuevoServicio> builder)
        {
            builder.Property(s => s.DireccionReferencia)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(s => s.Observacion)
                .HasMaxLength(1000);

            builder
                .HasOne(s => s.PersonaSolicitante)
                .WithMany()
                .HasForeignKey(s => s.PersonaSolicitanteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne(s => s.Sector)
                .WithMany()
                .HasForeignKey(s => s.SectorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne(s => s.UsuarioResolucion)
                .WithMany()
                .HasForeignKey(s => s.UsuarioResolucionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne(s => s.Suministro)
                .WithMany()
                .HasForeignKey(s => s.SuministroId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => new { s.Estado, s.FechaSolicitud });
        }
    }
}