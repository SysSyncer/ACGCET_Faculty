namespace ACGCET_Faculty.Models
{
    public class Batch
    {
        public int BatchId { get; set; }
        public int BatchYear { get; set; }
        public string BatchName { get; set; } = null!;
        public int? CourseId { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? ExpectedEndDate { get; set; }

        public virtual Course? Course { get; set; }
    }
}
