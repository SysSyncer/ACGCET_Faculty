namespace ACGCET_Faculty.Models
{
    public class Section
    {
        public int SectionId { get; set; }
        public string SectionName { get; set; } = null!;
        public string SectionCode { get; set; } = null!;
        public int? BatchId { get; set; }
        public int? MaxStudents { get; set; }

        public virtual Batch? Batch { get; set; }
    }
}
