namespace ACGCET_Faculty.Models
{
    public class DataCorrectionRequest
    {
        public int DataCorrectionRequestId { get; set; }
        public int? CorrectionRequestTypeId { get; set; }
        public int? RequestedBy { get; set; }
        public string TargetTable { get; set; } = null!;
        public string? TargetRecordId { get; set; }
        public string? TargetRecordDetails { get; set; }
        public string? CurrentValue { get; set; }
        public string? ProposedValue { get; set; }
        public string Reason { get; set; } = null!;
        public string? ImpactAnalysis { get; set; }
        public DateTime? RequestedDateTime { get; set; }
        public string? ApprovalStatus { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovalDateTime { get; set; }
        public string? ApprovalComments { get; set; }
        public int? ExecutedBy { get; set; }
        public DateTime? ExecutedDateTime { get; set; }
        public string? ExecutionNotes { get; set; }
        public bool? IsActive { get; set; }

        public virtual CorrectionRequestType? CorrectionRequestType { get; set; }
        public virtual AdminUser? RequestedByNavigation { get; set; }
    }
}
