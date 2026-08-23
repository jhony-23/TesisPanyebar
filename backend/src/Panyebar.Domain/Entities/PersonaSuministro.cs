using System;
using Panyebar.Domain.Enums;

namespace Panyebar.Domain.Entities
{
    /// <summary>
    /// Entidad que relaciona una Persona con un Suministro en un periodo determinado.
    /// </summary>
    public class PersonaSuministro
    {
        public int Id { get; set; }

        public int PersonaId { get; set; }
        public int SuministroId { get; set; }

        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        // Estado inicial por defecto
        public EstadoRelacionSuministro Estado { get; set; } = EstadoRelacionSuministro.Vigente;

        // Navegaciones. Nullable para evitar crear instancias ficticias y permitir relaciones históricas.
        public Persona? Persona { get; set; }
        public Suministro? Suministro { get; set; }
    }
}
