using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de relaciones para la entidad Persona.
    /// </summary>
    public class PersonaConfiguration : IEntityTypeConfiguration<Persona>
    {
        public void Configure(EntityTypeBuilder<Persona> builder)
        {
            builder.Property(p => p.Nombres)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(p => p.Apellidos)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(p => p.Identificacion)
                .HasMaxLength(50)
                .UseCollation("Latin1_General_100_CI_AS");

            builder.Property(p => p.Telefono)
                .HasMaxLength(30);

            builder.Property(p => p.DireccionReferencia)
                .HasMaxLength(500);

            builder.HasOne(p => p.Sector)
                .WithMany()
                .HasForeignKey(p => p.SectorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => p.Identificacion)
                .IsUnique()
                .HasFilter("[Identificacion] IS NOT NULL");

            // Relación 2: Persona → PersonaSuministro (1 : N)
            builder
                .HasMany<PersonaSuministro>()
                .WithOne(ps => ps.Persona)
                .HasForeignKey(ps => ps.PersonaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación 4: Persona → Obligacion (1 : N, optional)
            builder
                .HasMany<Obligacion>()
                .WithOne(o => o.Persona)
                .HasForeignKey(o => o.PersonaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación 8: Persona → ParticipacionJornada (1 : N)
            builder
                .HasMany<ParticipacionJornada>()
                .WithOne(pj => pj.Persona)
                .HasForeignKey(pj => pj.PersonaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación 20: Persona → IntegranteAdministracion (1 : N)
            builder
                .HasMany<IntegranteAdministracion>()
                .WithOne(ia => ia.Persona)
                .HasForeignKey(ia => ia.PersonaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
