using Panyebar.Domain.Enums;

namespace Panyebar.Domain.Entities
{
    public class Cargo
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;
    }
}
