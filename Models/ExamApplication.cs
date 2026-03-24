using System.Collections.Generic;

namespace ACGCET_Faculty.Models
{
    public class ExamApplication
    {
        public int ExamApplicationId { get; set; }
        public int StudentId { get; set; }
        public int ExaminationId { get; set; }
        public DateTime? ApplicationDate { get; set; }
        public decimal? TotalFees { get; set; }
        public bool? IsPaid { get; set; }
        public string? ApprovalStatus { get; set; }

        public virtual Student Student { get; set; } = null!;
        public virtual Examination Examination { get; set; } = null!;
        public virtual ICollection<ExamApplicationPaper> ExamApplicationPapers { get; set; } = new List<ExamApplicationPaper>();
    }
}
