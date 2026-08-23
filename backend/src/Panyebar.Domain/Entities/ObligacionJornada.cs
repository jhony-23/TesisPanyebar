namespace Panyebar.Domain.Entities
{
    /// <summary>
    /// Relaciona una participación o ausencia con la obligación económica generada cuando corresponda.
    /// </summary>
    public class ObligacionJornada
    {
        public int Id { get; set; }

        public int ParticipacionJornadaId { get; set; }
        public int ObligacionId { get; set; }

        public ParticipacionJornada? ParticipacionJornada { get; set; }
        public Obligacion? Obligacion { get; set; }
    }
}
