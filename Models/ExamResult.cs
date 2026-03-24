namespace ACGCET_Faculty.Models
{
    public class ExamResult
    {
        public long ExamResultId { get; set; }
        public int? StudentId { get; set; }
        public int? PaperId { get; set; }
        public int? ExaminationId { get; set; }
        public decimal? InternalTotal { get; set; }
        public decimal? ExternalTotal { get; set; }
        public decimal? GrandTotal { get; set; }
        public string? Grade { get; set; }
        public int? ResultStatusId { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string? CreatedBy { get; set; }

        public virtual Student? Student { get; set; }
        public virtual PaperSet? Paper { get; set; }
        public virtual Examination? Examination { get; set; }
        public virtual ResultStatus? ResultStatus { get; set; }
    }
}
