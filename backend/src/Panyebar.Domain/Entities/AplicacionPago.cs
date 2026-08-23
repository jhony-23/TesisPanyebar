using Panyebar.Domain.Enums;

namespace Panyebar.Domain.Entities
{
    /// <summary>
    /// Relaciona un pago con una obligación completa cancelada por esa transacción.
    /// </summary>
    public class AplicacionPago
    {
        public int Id { get; set; }

        public int PagoId { get; set; }
        public int ObligacionId { get; set; }

        public Pago? Pago { get; set; }
        public Obligacion? Obligacion { get; set; }
    }
}
