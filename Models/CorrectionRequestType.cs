namespace ACGCET_Faculty.Models
{
    public class CorrectionRequestType
    {
        public int CorrectionRequestTypeId { get; set; }
        public string TypeName { get; set; } = null!;
        public string TypeCode { get; set; } = null!;
        public bool? RequiresCoeapproval { get; set; }
        public bool? IsActive { get; set; }
    }
}
