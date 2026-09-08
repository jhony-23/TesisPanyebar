using Panyebar.Domain.Enums;

namespace Panyebar.Domain.Entities
{
    public class ProcesoSuministro
    {
        public int Id { get; set; }

        public int SuministroId { get; set; }
        public TipoProcesoSuministro TipoProceso { get; set; }
        public EstadoSuministro EstadoAnterior { get; set; }
        public EstadoSuministro EstadoNuevo { get; set; }
        public DateTime Fecha { get; set; }
        public int UsuarioAdministrativoId { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string? Observacion { get; set; }

        public Suministro? Suministro { get; set; }
        public UsuarioAdministrativo? UsuarioAdministrativo { get; set; }
    }
}