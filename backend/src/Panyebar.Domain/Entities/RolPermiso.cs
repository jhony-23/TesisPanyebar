namespace Panyebar.Domain.Entities
{
    public class RolPermiso
    {
        public int Id { get; set; }

        public int RolId { get; set; }

        public int PermisoId { get; set; }

        public Rol? Rol { get; set; }

        public Permiso? Permiso { get; set; }
    }
}
