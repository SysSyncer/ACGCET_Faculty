namespace ACGCET_Faculty.Models
{
    public class Regulation
    {
        public int RegulationId { get; set; }
        public int RegulationYear { get; set; }
        public string RegulationName { get; set; } = null!;
        public DateOnly? EffectiveFrom { get; set; }
    }
}
