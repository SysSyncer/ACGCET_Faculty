namespace ACGCET_Faculty.Models
{
    public class Degree
    {
        public int DegreeId { get; set; }
        public string DegreeName { get; set; } = null!;
        public string DegreeCode { get; set; } = null!;
        public string? GraduationLevel { get; set; } // UG, PG
    }
}
