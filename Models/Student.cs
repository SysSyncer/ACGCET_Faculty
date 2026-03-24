namespace ACGCET_Faculty.Models
{
    public class Student
    {
        public int StudentId { get; set; }
        public string? AdmissionNumber { get; set; }
        public string? RollNumber { get; set; }
        public string? RegistrationNumber { get; set; }
        public string FullName { get; set; } = null!;
        public DateOnly? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? MobileNumber { get; set; }
        public int? CommunityId { get; set; }
        public int? CourseId { get; set; }
        public int? BatchId { get; set; }
        public int? SectionId { get; set; }
        public int? RegulationId { get; set; }
        public int? JoinYear { get; set; }

        public virtual Course?     Course     { get; set; }
        public virtual Batch?      Batch      { get; set; }
        public virtual Section?    Section    { get; set; }
        public virtual Regulation? Regulation { get; set; }
        public virtual Community?  Community  { get; set; }

        public override string ToString() => FullName;
    }
}
