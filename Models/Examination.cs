namespace ACGCET_Faculty.Models
{
    public class Examination
    {
        public int ExaminationId { get; set; }
        public string ExamCode { get; set; } = null!;
        public string? ExamMonth { get; set; }
        public int? ExamYear { get; set; }
        public int? ExamTypeId { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public bool? IsResultLocked { get; set; }

        public virtual ExamType? ExamType { get; set; }

        public override string ToString() => ExamCode;
    }
}
