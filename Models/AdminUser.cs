using System;
using System.Collections.Generic;

namespace ACGCET_Faculty.Models
{
    public class AdminUser
    {
        public int AdminUserId { get; set; }
        public string UserName { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsLocked { get; set; }
        public int? FailedLoginAttempts { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime? CreatedDate { get; set; }

        public virtual ICollection<AdminUserRole> AdminUserRoles { get; set; } = new List<AdminUserRole>();
    }
}
