using System.Threading;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ACGCET_Faculty.Models
{
    /// <summary>
    /// Entity Framework DbContext for the Faculty application.
    /// Shares the same ACGCET_MASTER database as Admin_V2.
    /// 
    /// Key feature: SetSessionContext() sets SQL SESSION_CONTEXT so that
    /// database triggers (TR_InternalMarks_Audit, etc.) can read the
    /// current faculty's username and write it to AuditLog automatically.
    /// 
    /// DbLock serializes async access when the context is registered as Singleton.
    /// </summary>
    public class FacultyDbContext : DbContext
    {
        /// <summary>
        /// Serialize concurrent async access to this Singleton DbContext.
        /// Acquire before any async DB operation; release when done.
        /// </summary>
        public SemaphoreSlim DbLock { get; } = new SemaphoreSlim(1, 1);

        public FacultyDbContext(DbContextOptions<FacultyDbContext> options) : base(options) { }

        // ─── Auth ───────────────────────────────────────────────────────
        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<AdminUserRole> AdminUserRoles { get; set; }
        public DbSet<Role> Roles { get; set; }

        // ─── Academic Structure ─────────────────────────────────────────
        public DbSet<Degree> Degrees { get; set; }
        public DbSet<Program> Programs { get; set; }
        public DbSet<Regulation> Regulations { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Batch> Batches { get; set; }
        public DbSet<Section> Sections { get; set; }
        public DbSet<Scheme> Schemes { get; set; }

        // ─── Papers ─────────────────────────────────────────────────────
        public DbSet<PaperSet> PaperSet { get; set; }
        public DbSet<PaperType> PaperTypes { get; set; }
        public DbSet<PaperMarkDistribution> PaperMarkDistributions { get; set; }
        public DbSet<TestType> TestTypes { get; set; }

        // ─── Students ───────────────────────────────────────────────────
        public DbSet<Student> Students { get; set; }
        public DbSet<Community> Communities { get; set; }

        // ─── Exam ───────────────────────────────────────────────────────
        public DbSet<ExamType> ExamTypes { get; set; }
        public DbSet<Examination> Examinations { get; set; }
        public DbSet<ExamSession> ExamSessions { get; set; }
        public DbSet<ExamApplication> ExamApplications { get; set; }
        public DbSet<ExamApplicationPaper> ExamApplicationPapers { get; set; }

        // ─── Marks ──────────────────────────────────────────────────────
        public DbSet<InternalMark> InternalMarks { get; set; }

        // ─── Results ─────────────────────────────────────────────────────
        public DbSet<ExamResult> ExamResults { get; set; }
        public DbSet<ResultStatus> ResultStatuses { get; set; }

        // ─── Module Locks ────────────────────────────────────────────────
        public DbSet<Module> Modules { get; set; }
        public DbSet<ModuleLock> ModuleLocks { get; set; }

        // ─── Audit ──────────────────────────────────────────────────────
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SystemAlert> SystemAlerts { get; set; }
        public DbSet<AnomalyDetectionLog> AnomalyDetectionLogs { get; set; }

        // ─── Requests ────────────────────────────────────────────────────
        public DbSet<CorrectionRequestType> CorrectionRequestTypes { get; set; }
        public DbSet<DataCorrectionRequest> DataCorrectionRequests { get; set; }
        public DbSet<LockOverrideRequest> LockOverrideRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Map table names where EF pluralization differs
            modelBuilder.Entity<AdminUser>().ToTable("AdminUsers");
            modelBuilder.Entity<AdminUserRole>().ToTable("AdminUserRoles");
            modelBuilder.Entity<Role>().ToTable("Roles");
            modelBuilder.Entity<Degree>().ToTable("Degrees");
            modelBuilder.Entity<Program>().ToTable("Programs");
            modelBuilder.Entity<Regulation>().ToTable("Regulations");
            modelBuilder.Entity<Course>().ToTable("Courses");
            modelBuilder.Entity<Batch>().ToTable("Batches");
            modelBuilder.Entity<Section>().ToTable("Sections");
            modelBuilder.Entity<Scheme>().ToTable("Schemes");
            modelBuilder.Entity<PaperSet>().ToTable("Papers");
            modelBuilder.Entity<PaperType>().ToTable("PaperTypes");
            modelBuilder.Entity<PaperMarkDistribution>().ToTable("PaperMarkDistribution");
            modelBuilder.Entity<TestType>().ToTable("TestTypes");
            modelBuilder.Entity<Student>().ToTable("Students", t => t.HasTrigger("TR_Students_Audit"));
            modelBuilder.Entity<Community>().ToTable("Communities");
            modelBuilder.Entity<ExamType>().ToTable("ExamTypes");
            modelBuilder.Entity<Examination>().ToTable("Examinations");
            modelBuilder.Entity<ExamSession>().ToTable("ExamSessions");
            modelBuilder.Entity<ExamApplication>().ToTable("ExamApplications", t => t.HasTrigger("TR_ExamApplications_Audit"));
            modelBuilder.Entity<ExamApplicationPaper>().ToTable("ExamApplicationPapers");
            modelBuilder.Entity<InternalMark>().ToTable("InternalMarks", t => t.HasTrigger("TR_InternalMarks_Audit"));
            modelBuilder.Entity<ExamResult>().ToTable("ExamResults");
            modelBuilder.Entity<ResultStatus>().ToTable("ResultStatuses");
            modelBuilder.Entity<Module>().ToTable("Modules");
            modelBuilder.Entity<ModuleLock>().ToTable("ModuleLocks");
            modelBuilder.Entity<AuditLog>().ToTable("AuditLog");
            modelBuilder.Entity<SystemAlert>().ToTable("SystemAlerts");
            modelBuilder.Entity<AnomalyDetectionLog>().ToTable("AnomalyDetectionLog");

            // Request tables
            modelBuilder.Entity<CorrectionRequestType>().ToTable("CorrectionRequestTypes");
            modelBuilder.Entity<DataCorrectionRequest>().ToTable("DataCorrectionRequests");
            modelBuilder.Entity<LockOverrideRequest>().ToTable("LockOverrideRequests");

            // AdminUser unique indexes
            modelBuilder.Entity<AdminUser>().HasIndex(e => e.UserName).IsUnique();

            // AdminUserRole composite unique
            modelBuilder.Entity<AdminUserRole>()
                .HasIndex(e => new { e.AdminUserId, e.RoleId })
                .IsUnique();

            // InternalMark composite unique
            modelBuilder.Entity<InternalMark>()
                .HasIndex(e => new { e.StudentId, e.PaperId, e.TestTypeId, e.Semester })
                .IsUnique();

            // ModuleLock composite unique
            modelBuilder.Entity<ModuleLock>()
                .HasIndex(e => new { e.ModuleId, e.ExaminationId })
                .IsUnique();

            // Relationships
            modelBuilder.Entity<AdminUserRole>()
                .HasOne(d => d.AdminUser).WithMany(p => p.AdminUserRoles)
                .HasForeignKey(d => d.AdminUserId);

            modelBuilder.Entity<AdminUserRole>()
                .HasOne(d => d.Role).WithMany()
                .HasForeignKey(d => d.RoleId);

            modelBuilder.Entity<InternalMark>()
                .HasOne(d => d.Student).WithMany()
                .HasForeignKey(d => d.StudentId);

            modelBuilder.Entity<InternalMark>()
                .HasOne(d => d.Paper).WithMany()
                .HasForeignKey(d => d.PaperId);

            modelBuilder.Entity<InternalMark>()
                .HasOne(d => d.TestType).WithMany()
                .HasForeignKey(d => d.TestTypeId);

            modelBuilder.Entity<Student>()
                .HasOne(d => d.Batch).WithMany().HasForeignKey(d => d.BatchId);
            modelBuilder.Entity<Student>()
                .HasOne(d => d.Section).WithMany().HasForeignKey(d => d.SectionId);
            modelBuilder.Entity<Student>()
                .HasOne(d => d.Course).WithMany().HasForeignKey(d => d.CourseId);
            modelBuilder.Entity<Student>()
                .HasOne(d => d.Regulation).WithMany().HasForeignKey(d => d.RegulationId);

            modelBuilder.Entity<ExamApplicationPaper>()
                .HasOne(d => d.ExamApplication).WithMany(p => p.ExamApplicationPapers)
                .HasForeignKey(d => d.ExamApplicationId);
            modelBuilder.Entity<ExamApplicationPaper>()
                .HasOne(d => d.Paper).WithMany()
                .HasForeignKey(d => d.PaperId);

            modelBuilder.Entity<ExamApplication>()
                .HasOne(d => d.Student).WithMany()
                .HasForeignKey(d => d.StudentId);

            modelBuilder.Entity<PaperSet>()
                .HasOne(d => d.Course).WithMany()
                .HasForeignKey(d => d.CourseId);

            modelBuilder.Entity<Batch>()
                .HasOne(d => d.Course).WithMany()
                .HasForeignKey(d => d.CourseId);

            modelBuilder.Entity<Section>()
                .HasOne(d => d.Batch).WithMany()
                .HasForeignKey(d => d.BatchId);

            modelBuilder.Entity<Course>()
                .HasOne(d => d.Program).WithMany()
                .HasForeignKey(d => d.ProgramId);
            modelBuilder.Entity<Course>()
                .HasOne(d => d.Degree).WithMany()
                .HasForeignKey(d => d.DegreeId);
            modelBuilder.Entity<Course>()
                .HasOne(d => d.Regulation).WithMany()
                .HasForeignKey(d => d.RegulationId);

            modelBuilder.Entity<Program>()
                .HasOne(d => d.Degree).WithMany()
                .HasForeignKey(d => d.DegreeId);

            modelBuilder.Entity<ModuleLock>()
                .HasOne(d => d.Module).WithMany()
                .HasForeignKey(d => d.ModuleId);

            modelBuilder.Entity<ExamResult>()
                .HasOne(d => d.Student).WithMany()
                .HasForeignKey(d => d.StudentId);
            modelBuilder.Entity<ExamResult>()
                .HasOne(d => d.Paper).WithMany()
                .HasForeignKey(d => d.PaperId);
            modelBuilder.Entity<ExamResult>()
                .HasOne(d => d.Examination).WithMany()
                .HasForeignKey(d => d.ExaminationId);
            modelBuilder.Entity<ExamResult>()
                .HasOne(d => d.ResultStatus).WithMany()
                .HasForeignKey(d => d.ResultStatusId);

            modelBuilder.Entity<ExamResult>()
                .HasIndex(e => new { e.StudentId, e.PaperId, e.ExaminationId })
                .IsUnique();

            // PaperMarkDistribution: FK is PaperId on the dependent side (PaperMarkDistribution),
            // not a shadow PaperMarkDistributionId column on Papers. Without this config EF
            // generates wrong SQL looking for Papers.PaperMarkDistributionId.
            modelBuilder.Entity<PaperMarkDistribution>()
                .HasIndex(e => e.PaperId)
                .IsUnique();
            modelBuilder.Entity<PaperSet>()
                .HasOne(p => p.PaperMarkDistribution)
                .WithOne()
                .HasForeignKey<PaperMarkDistribution>(d => d.PaperId);

            // DataCorrectionRequest relationships
            modelBuilder.Entity<DataCorrectionRequest>()
                .HasOne(d => d.CorrectionRequestType).WithMany()
                .HasForeignKey(d => d.CorrectionRequestTypeId);
            modelBuilder.Entity<DataCorrectionRequest>()
                .HasOne(d => d.RequestedByNavigation).WithMany()
                .HasForeignKey(d => d.RequestedBy);

            // LockOverrideRequest relationships
            modelBuilder.Entity<LockOverrideRequest>()
                .HasOne(d => d.ModuleLock).WithMany()
                .HasForeignKey(d => d.ModuleLockId);
            modelBuilder.Entity<LockOverrideRequest>()
                .HasOne(d => d.RequestedByNavigation).WithMany()
                .HasForeignKey(d => d.RequestedBy);
        }

        /// <summary>
        /// Sets SQL SESSION_CONTEXT with the current faculty's username.
        /// This allows the database triggers (TR_InternalMarks_Audit, etc.) to
        /// read CAST(SESSION_CONTEXT(N'CurrentUserName') AS NVARCHAR(100))
        /// and log the correct user in AuditLog.EnteredBy / AuditLog.AdminUserId.
        /// 
        /// Call this ONCE after login, before any data modifications.
        /// </summary>
        public void SetSessionContext(string username)
        {
            if (string.IsNullOrEmpty(username)) return;
            try
            {
                // Opens a raw ADO.NET command on EF Core's underlying connection
                var connection = Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                    connection.Open();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "EXEC sp_set_session_context N'CurrentUserName', @username";
                var param = cmd.CreateParameter();
                param.ParameterName = "@username";
                param.Value = username;
                cmd.Parameters.Add(param);
                cmd.ExecuteNonQuery();
            }
            catch
            {
                // Non-fatal — triggers will fall back to SYSTEM_USER if SESSION_CONTEXT is null
            }
        }
    }
}
