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

        public DateTime Fecha { get; set; }

        public string Descripcion { get; set; } = string.Empty;
        public decimal? MontoIncumplimiento { get; set; }

        public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;
    }
}
