using Panyebar.Domain.Enums;

namespace Panyebar.Domain.Entities
{
    /// <summary>
    /// Representa un suministro de agua.
    /// Contiene NIS y token QR como propiedades del suministro.
    /// </summary>
    public class Suministro
    {
        public int Id { get; set; }

        public int SectorId { get; set; }

        // Propiedades del suministro (no nulas inicializadas de forma segura)
        public string Nis { get; set; } = string.Empty;
        public string CodigoQrToken { get; set; } = string.Empty;
        public string DireccionReferencia { get; set; } = string.Empty;

        // Estado inicial por defecto
        public EstadoSuministro Estado { get; set; } = EstadoSuministro.Activo;

        // Navegación hacia Sector. Nullable para permitir existencia independiente y evitar instancias ficticias.
        public Sector? Sector { get; set; }
    }
}
