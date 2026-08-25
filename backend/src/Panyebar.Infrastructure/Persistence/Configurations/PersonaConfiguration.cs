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
