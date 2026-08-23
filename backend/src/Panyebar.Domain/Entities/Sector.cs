using Panyebar.Domain.Enums;

namespace Panyebar.Domain.Entities
{
    /// <summary>
    /// Representa un sector configurable en el dominio.
    /// </summary>
    public class Sector
    {
        public int Id { get; set; }

        // Nombre inicializado de forma segura
        public string Nombre { get; set; } = string.Empty;

        // Descripción opcional
        public string? Descripcion { get; set; }

        // Estado de registro, valor inicial Activo
        public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;
    }
}
