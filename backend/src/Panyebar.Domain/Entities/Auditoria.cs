namespace Panyebar.Domain.Entities
{
    public class Auditoria
    {
        public int Id { get; set; }

        public int UsuarioAdministrativoId { get; set; }

        public string Accion { get; set; } = string.Empty;

        public string Entidad { get; set; } = string.Empty;

        public int EntidadId { get; set; }

        public DateTime Fecha { get; set; }

        public string? ValorAnterior { get; set; }

        public string? ValorNuevo { get; set; }

        public UsuarioAdministrativo? UsuarioAdministrativo { get; set; }
    }
}
