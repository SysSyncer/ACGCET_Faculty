namespace ACGCET_Faculty.Models
{
    /// <summary>
    /// InternalMark — one row per (Student, Paper, TestType, Semester).
    /// EnteredBy / ModifiedBy are set by the SQL trigger via SESSION_CONTEXT.
    /// In the application we also set these fields directly for clarity.
    /// </summary>
    public class InternalMark
    {
        public long InternalMarkId { get; set; }
        public int  StudentId     { get; set; }
        public int  PaperId       { get; set; }
        public int  TestTypeId    { get; set; }
        public int? Semester      { get; set; }
        public decimal? Mark      { get; set; }
        public decimal? MaxMark   { get; set; }

        // Audit fields — also written by SQL trigger TR_InternalMarks_Audit
        public string?   EnteredBy    { get; set; }
        public DateTime? EnteredDate  { get; set; }
        public string?   ModifiedBy   { get; set; }
        public DateTime? ModifiedDate { get; set; }

        public virtual Student  Student  { get; set; } = null!;
        public virtual PaperSet Paper    { get; set; } = null!;
        public virtual TestType TestType { get; set; } = null!;
    }
}
