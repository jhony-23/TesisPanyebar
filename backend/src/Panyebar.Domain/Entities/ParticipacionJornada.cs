using Panyebar.Domain.Enums;

namespace Panyebar.Domain.Entities
{
    /// <summary>
    /// Registra la participación o ausencia de una persona en una jornada.
    /// </summary>
    public class ParticipacionJornada
    {
        public int Id { get; set; }

        public int JornadaId { get; set; }
        public int PersonaId { get; set; }

        public ResultadoParticipacionJornada Resultado { get; set; }
        public string? Observacion { get; set; }

        public Jornada? Jornada { get; set; }
        public Persona? Persona { get; set; }
    }
}
