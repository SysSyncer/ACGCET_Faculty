namespace ACGCET_Faculty.Models
{
    public class ExamSession
    {
        public int ExamSessionId { get; set; }
        public string SessionName { get; set; } = null!;
        public string SessionCode { get; set; } = null!;
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
    }
}
