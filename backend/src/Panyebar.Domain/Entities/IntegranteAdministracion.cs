namespace Panyebar.Domain.Entities
{
    public class IntegranteAdministracion
    {
        public int Id { get; set; }

        public int AdministracionComiteId { get; set; }

        public int PersonaId { get; set; }

        public int CargoId { get; set; }

        public AdministracionComite? AdministracionComite { get; set; }

        public Persona? Persona { get; set; }

        public Cargo? Cargo { get; set; }
    }
}
