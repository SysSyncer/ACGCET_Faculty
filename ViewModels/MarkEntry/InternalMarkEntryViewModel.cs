using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ACGCET_Faculty.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace ACGCET_Faculty.ViewModels.MarkEntry
{
    /// <summary>
    /// MarkEntryRow is a helper model for the UI DataGrid.
    /// It holds one student and their marks across all TestTypes in one row.
    /// </summary>
    public partial class MarkEntryRow : ObservableObject
    {
        public int StudentId { get; set; }
        public string RegistrationNumber { get; set; } = "";
        public string RollNumber { get; set; } = "";
        public string FullName { get; set; } = "";

        // Marks per TestType — UI binds to these
        [ObservableProperty] private string? _markCIA1;
        [ObservableProperty] private string? _markCIA2;
        [ObservableProperty] private string? _markCIA3;
        [ObservableProperty] private string? _markModel;

        // Existing InternalMark IDs, if the row already has data (for UPDATE vs INSERT)
        public long? ExistingCIA1Id { get; set; }
        public long? ExistingCIA2Id { get; set; }
        public long? ExistingCIA3Id { get; set; }
        public long? ExistingModelId { get; set; }

        // Visual helpers
        public bool IsModified { get; set; }
    }

    /// <summary>
    /// Core mark entry screen.
    /// Flow:
    ///  1. Cascading dropdowns: Course Level → Program → Batch → Section → Semester → Paper
    ///  2. On Load Students: fetches students registered for the selected paper
    ///  3. Shows existing marks if already entered (pre-fills grid)
    ///  4. Faculty types/edits marks
    ///  5. Save: checks module lock → validates → upserts InternalMarks
    ///     SESSION_CONTEXT is already set from login, so SQL trigger auto-logs to AuditLog
    /// </summary>
    public partial class InternalMarkEntryViewModel : ObservableObject
    {
        private readonly FacultyDbContext _db;
        private readonly AdminUser _currentUser;

        // ─── Cascading Dropdowns ─────────────────────────────────────────
        [ObservableProperty] private ObservableCollection<Course> _courses = new();
        [ObservableProperty] private ObservableCollection<Batch> _batches = new();
        [ObservableProperty] private ObservableCollection<Section> _sections = new();
        [ObservableProperty] private ObservableCollection<PaperSet> _papers = new();
        [ObservableProperty] private ObservableCollection<int> _semesters = new();

        [ObservableProperty] private Course? _selectedCourse;
        [ObservableProperty] private Batch? _selectedBatch;
        [ObservableProperty] private Section? _selectedSection;
        [ObservableProperty] private PaperSet? _selectedPaper;
        [ObservableProperty] private int? _selectedSemester;

        // TestTypes (IA1, IA2, IA3, Model)
        private List<TestType> _testTypes = new();

        // ─── Module Lock ─────────────────────────────────────────────────
        [ObservableProperty] private bool _isModuleLocked = false;
        [ObservableProperty] private string _lockMessage = "";
        [ObservableProperty] private bool _canSave = true;

        // ─── Student Grid ────────────────────────────────────────────────
        [ObservableProperty]
        private ObservableCollection<MarkEntryRow> _students = new();

        // ─── Status ──────────────────────────────────────────────────────
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotLoading))]
        private bool _isLoading = false;
        public bool IsNotLoading => !IsLoading;
        [ObservableProperty] private string _statusMessage = "";
        [ObservableProperty] private string _statusColor = "#4CAF50";
        [ObservableProperty] private bool _hasStudents = false;
        // ─── Lock Override Request ───────────────────────────────────────
        [ObservableProperty] private string _lockOverrideReason = "";
        [ObservableProperty] private bool _isLockOverridePanelVisible = false;
        public InternalMarkEntryViewModel(FacultyDbContext db, AdminUser user)
        {
            _db = db;
            _currentUser = user;
        }

        public async Task InitializeAsync()
        {
            await CheckModuleLockAsync();
            await LoadCoursesAsync();
            await LoadTestTypesAsync();
        }

        // ─── Module Lock Check ───────────────────────────────────────────
        private async Task CheckModuleLockAsync()
        {
            try
            {
                var module = await _db.Modules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.ModuleCode == "INT_MARKS");

                if (module == null) { CanSave = true; return; }

                var latestExamId = await _db.Examinations.AsNoTracking()
                    .OrderByDescending(e => e.ExaminationId).Select(e => (int?)e.ExaminationId)
                    .FirstOrDefaultAsync();
                var locked = await _db.ModuleLocks
                    .AsNoTracking()
                    .AnyAsync(l => l.ModuleId == module.ModuleId && l.IsLocked == true
                        && (l.ExaminationId == null || l.ExaminationId == latestExamId));

                IsModuleLocked = locked;
                CanSave = !locked;
                LockMessage = locked
                    ? "⚠️  Internal Mark Entry is currently LOCKED by the COE. You cannot save marks until the module is unlocked."
                    : "";
            }
            catch { CanSave = true; }
        }

        // ─── Load Dropdowns ───────────────────────────────────────────────
        private async Task LoadCoursesAsync()
        {
            var list = await _db.Courses
                .Include(c => c.Program)
                .Include(c => c.Degree)
                .Include(c => c.Regulation)
                .ToListAsync();
            Courses = new ObservableCollection<Course>(list);
        }

        private async Task LoadTestTypesAsync()
        {
            _testTypes = await _db.TestTypes.ToListAsync();
        }

        partial void OnSelectedCourseChanged(Course? value)
        {
            SelectedBatch = null;
            SelectedSection = null;
            SelectedPaper = null;
            Students.Clear();
            HasStudents = false;
            if (value == null) return;
            _ = LoadBatchesAsync(value.CourseId);
            _ = LoadSemestersAsync(value);
        }

        private async Task LoadBatchesAsync(int courseId)
        {
            var list = await _db.Batches
                .Where(b => b.CourseId == courseId)
                .OrderByDescending(b => b.BatchYear)
                .ToListAsync();
            Batches = new ObservableCollection<Batch>(list);
        }

        private Task LoadSemestersAsync(Course course)
        {
            int total = course.TotalSemesters ?? 8;
            var sems = Enumerable.Range(1, total).ToList();
            Semesters = new ObservableCollection<int>(sems);
            return Task.CompletedTask;
        }

        partial void OnSelectedBatchChanged(Batch? value)
        {
            SelectedSection = null;
            Students.Clear();
            HasStudents = false;
            if (value == null) return;
            _ = LoadSectionsAsync(value.BatchId);
        }

        private async Task LoadSectionsAsync(int batchId)
        {
            var list = await _db.Sections
                .Where(s => s.BatchId == batchId)
                .ToListAsync();
            Sections = new ObservableCollection<Section>(list);
        }

        partial void OnSelectedSemesterChanged(int? value)
        {
            SelectedPaper = null;
            Students.Clear();
            HasStudents = false;
            if (value == null || SelectedCourse == null) return;
            _ = LoadPapersAsync(SelectedCourse.CourseId, value.Value);
        }

        private async Task LoadPapersAsync(int courseId, int semester)
        {
            var list = await _db.PaperSet
                .Where(p => p.CourseId == courseId && p.Semester == semester)
                .Include(p => p.PaperMarkDistribution)
                .OrderBy(p => p.PaperCode)
                .ToListAsync();
            Papers = new ObservableCollection<PaperSet>(list);
        }

        // ─── Load Students for selected Paper + Section ─────────────────
        [RelayCommand]
        private async Task LoadStudents()
        {
            if (SelectedPaper == null || SelectedSection == null)
            {
                SetStatus("Please select a Course, Batch, Section, Semester and Paper first.", "#FF9800");
                return;
            }

            IsLoading = true;
            Students.Clear();
            HasStudents = false;
            StatusMessage = "";

            try
            {
                // Get students in the selected section who have exam applications for this paper
                // (or fall back to all students in section for internal marks)
                var studentList = await _db.Students
                    .Where(s => s.SectionId == SelectedSection.SectionId &&
                                s.CourseId == SelectedCourse!.CourseId)
                    .Include(s => s.Regulation)
                    .OrderBy(s => s.RollNumber)
                    .ToListAsync();

                if (!studentList.Any())
                {
                    SetStatus("No students found for the selected section.", "#FF9800");
                    return;
                }

                // Load existing InternalMarks for this paper + semester
                var studentIds = studentList.Select(s => s.StudentId).ToList();
                var existingMarks = await _db.InternalMarks
                    .Where(m => m.PaperId == SelectedPaper.PaperId &&
                                m.Semester == SelectedSemester &&
                                studentIds.Contains(m.StudentId))
                    .Include(m => m.TestType)
                    .ToListAsync();

                // Build grid rows
                foreach (var student in studentList)
                {
                    var row = new MarkEntryRow
                    {
                        StudentId = student.StudentId,
                        RegistrationNumber = student.RegistrationNumber ?? "-",
                        RollNumber = student.RollNumber ?? "-",
                        FullName = student.FullName,
                    };

                    // Pre-fill existing marks
                    foreach (var mark in existingMarks.Where(m => m.StudentId == student.StudentId))
                    {
                        switch (mark.TestType?.TestCode?.ToUpper())
                        {
                            case "CIAT1": row.MarkCIA1 = mark.Mark?.ToString(); row.ExistingCIA1Id = mark.InternalMarkId; break;
                            case "CIAT2": row.MarkCIA2 = mark.Mark?.ToString(); row.ExistingCIA2Id = mark.InternalMarkId; break;
                            case "CIAT3": row.MarkCIA3 = mark.Mark?.ToString(); row.ExistingCIA3Id = mark.InternalMarkId; break;
                            case "MODEL": row.MarkModel = mark.Mark?.ToString(); row.ExistingModelId = mark.InternalMarkId; break;
                        }
                    }

                    Students.Add(row);
                }

                HasStudents = true;
                SetStatus($"Loaded {Students.Count} students. Existing marks pre-filled.", "#4CAF50");
            }
            catch (Exception ex)
            {
                SetStatus($"Error loading students: {ex.Message}", "#F44336");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ─── Save Marks ──────────────────────────────────────────────────
        [RelayCommand]
        private async Task SaveMarks()
        {
            // Re-check module lock before saving
            await CheckModuleLockAsync();
            if (IsModuleLocked)
            {
                // Log anomaly — attempted save while module is locked
                try
                {
                    _db.Set<AnomalyDetectionLog>().Add(new AnomalyDetectionLog
                    {
                        DetectionType = "LOCKED_MODULE_SAVE_ATTEMPT",
                        SuspiciousUserId = _currentUser.AdminUserId,
                        TargetTable = "InternalMarks",
                        TargetRecordId = SelectedPaper?.PaperId.ToString(),
                        DetectionDateTime = DateTime.Now,
                        DetectionReason = $"User '{_currentUser.UserName}' attempted to save internal marks while INT_MARKS module is locked.",
                        SeverityLevel = "High",
                        IsInvestigated = false
                    });
                    await _db.SaveChangesAsync();
                }
                catch { /* non-fatal — don't block the error message */ }

                SetStatus("Cannot save — module is locked by COE.", "#F44336");
                return;
            }

            if (!Students.Any())
            {
                SetStatus("No students loaded. Please load students first.", "#FF9800");
                return;
            }

            IsLoading = true;
            int saved = 0, updated = 0, deleted = 0, errors = 0;

            try
            {
                var paper = SelectedPaper!;
                var semester = SelectedSemester ?? 1;
                var now = DateTime.Now;
                var enteredBy = _currentUser.UserName;

                // Map TestType codes to IDs (Matching database CIAT1, CIAT2, CIAT3 codes)
                var cia1 = _testTypes.FirstOrDefault(t => t.TestCode.ToUpper() == "CIAT1");
                var cia2 = _testTypes.FirstOrDefault(t => t.TestCode.ToUpper() == "CIAT2");
                var cia3 = _testTypes.FirstOrDefault(t => t.TestCode.ToUpper() == "CIAT3");
                var model = _testTypes.FirstOrDefault(t => t.TestCode.ToUpper() == "MODEL");

                decimal maxMark = paper.PaperMarkDistribution?.InternalTheoryMax ?? 50m;

                foreach (var row in Students)
                {
                    // Helper to upsert a single TestType mark
                    async Task Upsert(string? markStr, TestType? testType, long? existingId)
                    {
                        if (testType == null) return;

                        // Handle Clearing/Deletion: If input is empty and record exists, delete it
                        if (string.IsNullOrWhiteSpace(markStr))
                        {
                            if (existingId.HasValue)
                            {
                                var existing = await _db.InternalMarks.FindAsync(existingId.Value);
                                if (existing != null)
                                {
                                    _db.InternalMarks.Remove(existing);
                                    deleted++;
                                }
                            }
                            return;
                        }

                        if (!decimal.TryParse(markStr, out decimal mark))
                        { errors++; return; }

                        // Use specific TestType max mark if available, else fallback to paper max
                        decimal currentMax = testType.MaxMark > 0 ? (decimal)testType.MaxMark : maxMark;
                        if (mark < 0 || mark > currentMax)
                        { errors++; return; }

                        if (existingId.HasValue)
                        {
                            // UPDATE
                            var existing = await _db.InternalMarks.FindAsync(existingId.Value);
                            if (existing != null)
                            {
                                existing.Mark = mark;
                                existing.ModifiedBy = enteredBy;
                                existing.ModifiedDate = now;
                                updated++;
                            }
                        }
                        else
                        {
                            // INSERT
                            await _db.InternalMarks.AddAsync(new InternalMark
                            {
                                StudentId = row.StudentId,
                                PaperId = paper.PaperId,
                                TestTypeId = testType.TestTypeId,
                                Semester = semester,
                                Mark = mark,
                                MaxMark = currentMax,
                                EnteredBy = enteredBy,
                                EnteredDate = now
                            });
                            saved++;
                        }
                    }

                    await Upsert(row.MarkCIA1, cia1, row.ExistingCIA1Id);
                    await Upsert(row.MarkCIA2, cia2, row.ExistingCIA2Id);
                    await Upsert(row.MarkCIA3, cia3, row.ExistingCIA3Id);
                    await Upsert(row.MarkModel, model, row.ExistingModelId);
                }

                // SESSION_CONTEXT is already set (from Login), so TR_InternalMarks_Audit
                // will write the faculty username into AuditLog automatically.
                await _db.SaveChangesAsync();

                string msg = $"✅  Saved {saved} new, Updated {updated} and Removed {deleted} marks.";
                if (errors > 0) msg += $" ({errors} invalid values skipped)";
                SetStatus(msg, errors > 0 ? "#FF9800" : "#4CAF50");

                // Reload to refresh ExistingIDs after new inserts
                await LoadStudents();
            }
            catch (Exception ex)
            {
                string innerMsg = ex.InnerException?.Message ?? ex.Message;
                // SQL trigger may raise error if module gets locked mid-session
                if (innerMsg.Contains("locked", StringComparison.OrdinalIgnoreCase))
                {
                    IsModuleLocked = true;
                    CanSave = false;
                    LockMessage = "⚠️  Module was locked by COE during entry. Your unsaved changes were not applied.";
                }
                SetStatus($"Save failed: {innerMsg}", "#F44336");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void RequestLockOverride()
        {
            LockOverrideReason = "";
            IsLockOverridePanelVisible = true;
        }

        [RelayCommand]
        private void CancelLockOverride()
        {
            IsLockOverridePanelVisible = false;
            LockOverrideReason = "";
        }

        [RelayCommand]
        private async Task SubmitLockOverride()
        {
            if (string.IsNullOrWhiteSpace(LockOverrideReason))
            {
                SetStatus("Please provide a reason for the lock override request.", "#FF9800");
                return;
            }

            try
            {
                var module = await _db.Modules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.ModuleCode == "INT_MARKS");
                if (module == null)
                {
                    SetStatus("Module not found.", "#F44336");
                    return;
                }

                var moduleLock = await _db.ModuleLocks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.ModuleId == module.ModuleId && l.IsLocked == true);

                var request = new LockOverrideRequest
                {
                    ModuleLockId = moduleLock?.ModuleLockId,
                    RequestedBy = _currentUser.AdminUserId,
                    RequestReason = LockOverrideReason,
                    RequestedDateTime = DateTime.Now,
                    ApprovalStatus = "Pending",
                    IsActive = true
                };

                _db.LockOverrideRequests.Add(request);
                await _db.SaveChangesAsync();

                IsLockOverridePanelVisible = false;
                LockOverrideReason = "";
                SetStatus("Lock override request submitted. The COE will review your request.", "#4CAF50");
            }
            catch (Exception ex)
            {
                SetStatus($"Error submitting override request: {ex.Message}", "#F44336");
            }
        }

        [RelayCommand]
        private void ClearForm()
        {
            Students.Clear();
            HasStudents = false;
            StatusMessage = "";
            SelectedPaper = null;
        }

        private void SetStatus(string msg, string color)
        {
            StatusMessage = msg;
            StatusColor = color;
        }
    }
}
