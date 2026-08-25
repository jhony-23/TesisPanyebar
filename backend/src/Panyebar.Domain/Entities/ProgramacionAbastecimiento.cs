namespace Panyebar.Domain.Entities
{
    /// <summary>
    /// Representa la programación administrativa del abastecimiento de agua por sector.
    /// </summary>
    public class ProgramacionAbastecimiento
    {
        // Identificador interno de la programación
        public int Id { get; set; }

        // Referencia al sector al que corresponde la programación
        public int SectorId { get; set; }

        // Fecha correspondiente a la programación administrativa del abastecimiento
        public DateTime Fecha { get; set; }

        // Hora de inicio del intervalo programado
        public TimeSpan HoraInicio { get; set; }

        // Hora de finalización del intervalo programado
        public TimeSpan HoraFin { get; set; }

        // Estado administrativo de la programación
        public string Estado { get; set; } = string.Empty;

        // Observación administrativa opcional
        public string? Observacion { get; set; }

        // Propiedad de navegación hacia Sector
        public Sector? Sector { get; set; }
    }
}
