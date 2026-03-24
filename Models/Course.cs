namespace ACGCET_Faculty.Models
{
    public class Course
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = null!;
        public string CourseCode { get; set; } = null!;
        public int? DegreeId { get; set; }
        public int? ProgramId { get; set; }
        public int? RegulationId { get; set; }
        public int? DurationYears { get; set; }
        public int? TotalSemesters { get; set; }

        public virtual Degree? Degree { get; set; }
        public virtual Program? Program { get; set; }
        public virtual Regulation? Regulation { get; set; }
    }
}
