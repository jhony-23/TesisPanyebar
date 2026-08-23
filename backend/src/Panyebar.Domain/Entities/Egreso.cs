using System;

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

        public DateTime Fecha { get; set; }

        public int UsuarioAdministrativoId { get; set; }
    }
}
