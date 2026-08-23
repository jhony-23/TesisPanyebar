using System;
using Panyebar.Domain.Enums;

namespace Panyebar.Domain.Entities
{
    /// <summary>
    /// Representa la configuración de una cuota en el dominio.
    /// </summary>
    public class Cuota
    {
        public int Id { get; set; }

        public decimal Monto { get; set; }

        public PeriodicidadCuota Periodicidad { get; set; }

        public DateTime FechaInicioVigencia { get; set; }
        public DateTime? FechaFinVigencia { get; set; }

        public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;
    }
}
