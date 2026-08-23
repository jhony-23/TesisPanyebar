using System;
using Panyebar.Domain.Enums;

namespace Panyebar.Domain.Entities
{
    /// <summary>
    /// Representa una obligación económica generada para una persona o suministro.
    /// </summary>
    public class Obligacion
    {
        public int Id { get; set; }

        public int? PersonaId { get; set; }
        public int? SuministroId { get; set; }
        public int? CuotaId { get; set; }

        public OrigenObligacion Origen { get; set; }

        // Monto histórico de la obligación
        public decimal Monto { get; set; }

        // Período administrativo (por ejemplo "2026-01")
        public string Periodo { get; set; } = string.Empty;

        // Fecha en que se generó la obligación (proveída por el caso de uso)
        public DateTime FechaGeneracion { get; set; }

        // Estado inicial por defecto
        public EstadoObligacion Estado { get; set; } = EstadoObligacion.Pendiente;

        // Navegaciones opcionales
        public Persona? Persona { get; set; }
        public Suministro? Suministro { get; set; }
        public Cuota? Cuota { get; set; }
    }
}
