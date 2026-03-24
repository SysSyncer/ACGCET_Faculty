namespace ACGCET_Faculty.Models
{
    public class LockOverrideRequest
    {
        public int LockOverrideRequestId { get; set; }
        public int? ModuleLockId { get; set; }
        public int? RequestedBy { get; set; }
        public string RequestReason { get; set; } = null!;
        public DateTime? RequestedDateTime { get; set; }
        public int? ApprovedBy { get; set; }
        public string? ApprovalStatus { get; set; }
        public DateTime? ApprovalDateTime { get; set; }
        public string? ApprovalComments { get; set; }
        public int? TemporaryUnlockDuration { get; set; }
        public DateTime? TemporaryUnlockExpiry { get; set; }
        public bool? IsActive { get; set; }

        public virtual ModuleLock? ModuleLock { get; set; }
        public virtual AdminUser? RequestedByNavigation { get; set; }
    }
}
