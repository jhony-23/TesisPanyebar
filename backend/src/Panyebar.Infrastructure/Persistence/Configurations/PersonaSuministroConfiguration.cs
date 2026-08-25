using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Panyebar.Domain.Entities;
using Panyebar.Domain.Enums;

namespace Panyebar.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configuración de relaciones e índices para la entidad PersonaSuministro.
    /// </summary>
    public class PersonaSuministroConfiguration : IEntityTypeConfiguration<PersonaSuministro>
    {
        public void Configure(EntityTypeBuilder<PersonaSuministro> builder)
        {
            // Relaciones 2 y 3 se configuran desde PersonaConfiguration y SuministroConfiguration

            // Restricción 3: Un solo responsable Vigente por Suministro
            // Índice único filtrado: solo cuenta las relaciones Vigentes
            builder.HasIndex(ps => ps.SuministroId)
                .IsUnique()
                .HasFilter($"[Estado] = {(int)EstadoRelacionSuministro.Vigente}");
        }
    }
}
