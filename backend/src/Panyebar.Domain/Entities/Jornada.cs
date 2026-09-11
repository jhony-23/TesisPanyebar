using System;
using Panyebar.Domain.Enums;

namespace Panyebar.Domain.Entities
{
    /// <summary>
    /// Representa una jornada comunitaria aplicada a personas.
    /// </summary>
    public class Jornada
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }

        public DateTime Fecha { get; set; }
        public TimeOnly? HoraInicio { get; set; }
        public TimeOnly? HoraFin { get; set; }

        public string? Ubicacion { get; set; }
        public decimal? MontoIncumplimiento { get; set; }

        public EstadoJornada Estado { get; set; } = EstadoJornada.Planificada;
    }
}
