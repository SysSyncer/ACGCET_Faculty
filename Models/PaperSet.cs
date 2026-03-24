using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ACGCET_Faculty.Models
{
    // Named PaperSet to avoid collision with the C# keyword/namespace "Program"
    // The underlying SQL table is "Papers" and the PK column is "PaperID"
    [Table("Papers")]
    public class PaperSet
    {
        [Key]
        [Column("PaperID")]
        public int PaperId { get; set; }

        public string PaperCode { get; set; } = null!;
        public string PaperName { get; set; } = null!;
        public int? CourseId { get; set; }
        public int Semester { get; set; }
        public decimal? Credits { get; set; }
        public int? PaperTypeId { get; set; }
        public int? SchemeId { get; set; }

        public virtual Course? Course { get; set; }
        public virtual PaperType? PaperType { get; set; }
        public virtual Scheme? Scheme { get; set; }
        public virtual PaperMarkDistribution? PaperMarkDistribution { get; set; }

        public override string ToString() => $"{PaperCode} - {PaperName}";
    }
}
