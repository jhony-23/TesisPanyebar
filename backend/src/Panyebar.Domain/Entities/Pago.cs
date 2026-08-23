using System;
using Panyebar.Domain.Enums;

namespace Panyebar.Domain.Entities
{
    /// <summary>
    /// Representa una transacción de pago registrada en el dominio.
    /// </summary>
    public class Pago
    {
        public int Id { get; set; }

        public decimal Monto { get; set; }

        public DateTime Fecha { get; set; }

        public string Concepto { get; set; } = string.Empty;

        public int UsuarioAdministrativoId { get; set; }

        public EstadoPago Estado { get; set; } = EstadoPago.Registrado;
    }
}
