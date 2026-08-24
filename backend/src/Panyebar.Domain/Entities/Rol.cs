using Panyebar.Domain.Enums;

namespace Panyebar.Domain.Entities
{
    /// <summary>
    /// Representa un rol de autorización administrable en la plataforma.
    /// No debe contener información sobre cargos institucionales.
    /// </summary>
    public class Rol
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;
    }
}
