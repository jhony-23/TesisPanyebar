namespace Panyebar.Domain.Entities
{
    public class UsuarioRol
    {
        public int Id { get; set; }

        public int UsuarioAdministrativoId { get; set; }

        public int RolId { get; set; }

        public UsuarioAdministrativo? UsuarioAdministrativo { get; set; }

        public Rol? Rol { get; set; }
    }
}
