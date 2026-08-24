using Panyebar.Domain.Enums;

namespace Panyebar.Domain.Entities
{
    public class AdministracionComite
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public DateTime FechaInicio { get; set; }

        public DateTime? FechaFin { get; set; }

        public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;
    }
}
