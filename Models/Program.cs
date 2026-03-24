namespace ACGCET_Faculty.Models
{
    public class Program
    {
        public int ProgramId { get; set; }
        public string ProgramName { get; set; } = null!;
        public string ProgramCode { get; set; } = null!;
        public int? DegreeId { get; set; }

        public virtual Degree? Degree { get; set; }
    }
}
