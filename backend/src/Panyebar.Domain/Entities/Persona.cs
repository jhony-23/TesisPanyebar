using Panyebar.Domain.Enums;

namespace Panyebar.Domain.Entities
{
    /// <summary>
    /// Representa a una persona administrada por el sistema.
    /// Solo contiene las propiedades de dominio necesarias en este bloque.
    /// </summary>
    public class Persona
    {
        public int Id { get; set; }

        // Propiedades no anulables inicializadas de forma segura
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;

        // Propiedades opcionales
        public string? Identificacion { get; set; }
        public string? Telefono { get; set; }
        public string? DireccionReferencia { get; set; }

        // Estado de registro, valor inicial Activo
        public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;
    }
}
