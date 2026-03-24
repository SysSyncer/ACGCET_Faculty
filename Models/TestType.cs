namespace ACGCET_Faculty.Models
{
    public class TestType
    {
        public int TestTypeId { get; set; }
        public string TestName { get; set; } = null!;
        public string TestCode { get; set; } = null!;
        public decimal? MaxMark { get; set; }

        public override string ToString() => TestName;
    }
}
