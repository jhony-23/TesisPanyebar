using Panyebar.Domain.Enums;

namespace Panyebar.Domain.Entities
{
    /// <summary>
    /// Representa un permiso técnico administrable en la plataforma.
    /// Código identifica técnicamente el permiso; Nombre es su denominación visible.
    /// </summary>
    public class Permiso
    {
        public int Id { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;
    }
}
