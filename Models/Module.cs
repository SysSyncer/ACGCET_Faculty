namespace ACGCET_Faculty.Models
{
    public class Module
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; } = null!;
        public string ModuleCode { get; set; } = null!;
        public string? Description { get; set; }
        public bool? IsLockable { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
