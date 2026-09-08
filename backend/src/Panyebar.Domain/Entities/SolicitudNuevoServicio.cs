using Panyebar.Domain.Enums;

namespace Panyebar.Domain.Entities
{
    public class SolicitudNuevoServicio
    {
        public int Id { get; set; }

        public int PersonaSolicitanteId { get; set; }
        public int SectorId { get; set; }
        public string DireccionReferencia { get; set; } = string.Empty;
        public DateTime FechaSolicitud { get; set; }
        public EstadoSolicitudNuevoServicio Estado { get; set; } = EstadoSolicitudNuevoServicio.Pendiente;
        public DateTime? FechaResolucion { get; set; }
        public int? UsuarioResolucionId { get; set; }
        public string? Observacion { get; set; }
        public int? SuministroId { get; set; }

        public Persona? PersonaSolicitante { get; set; }
        public Sector? Sector { get; set; }
        public UsuarioAdministrativo? UsuarioResolucion { get; set; }
        public Suministro? Suministro { get; set; }
    }
}