namespace ACGCET_Faculty.Models
{
    public class SystemAlert
    {
        public int    SystemAlertId   { get; set; }
        public string AlertType       { get; set; } = null!;
        public string AlertSeverity   { get; set; } = null!;
        public string AlertMessage    { get; set; } = null!;
        public int?   RelatedUserId   { get; set; }
        public string? RelatedModule  { get; set; }
        public string? RelatedTable   { get; set; }
        public string? RelatedRecordId { get; set; }
        public DateTime AlertDateTime  { get; set; }
        public bool?  IsAcknowledged  { get; set; }
        public int?   AcknowledgedBy  { get; set; }
        public DateTime? AcknowledgedDateTime { get; set; }
        public bool?  IsResolved       { get; set; }
        public int?   ResolvedBy       { get; set; }
        public DateTime? ResolvedDateTime { get; set; }
        public string?   ResolutionNotes  { get; set; }
    }
}
