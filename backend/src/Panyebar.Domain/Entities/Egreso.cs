using System;
using Panyebar.Domain.Enums;

namespace Panyebar.Domain.Entities
{
    /// <summary>
    /// Representa un egreso administrativo del Comité de Agua Potable.
    /// </summary>
    public class Egreso
    {
        public int Id { get; set; }

        public string Concepto { get; set; } = string.Empty;

        public decimal Monto { get; set; }

        // Fecha civil del gasto; no representa un instante UTC.
        public DateTime Fecha { get; set; }

        public int UsuarioAdministrativoId { get; set; }

        public EstadoEgreso Estado { get; set; } = EstadoEgreso.Registrado;
    }
}
