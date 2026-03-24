using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ACGCET_Faculty.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace ACGCET_Faculty.ViewModels.ExamApplication
{
    /// <summary>
    /// Represents one student's exam application status for the selected examination.
    /// </summary>
    public partial class ExamAppRow : ObservableObject
    {
        public int StudentId { get; set; }
        public string RegistrationNumber { get; set; } = "";
        public string RollNumber { get; set; } = "";
        public string FullName { get; set; } = "";

        [ObservableProperty] private bool _isApplied;
        [ObservableProperty] private string _approvalStatus = "Not Applied";
        [ObservableProperty] private decimal? _totalFees;

        /// Set after INSERT; null means not yet registered in DB.
        private int? _existingApplicationId;
        public int? ExistingApplicationId
        {
            get => _existingApplicationId;
            set
            {
                _existingApplicationId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNew));
            }
        }

        /// True when this student has no existing application (checkbox enabled).
        public bool IsNew => !_existingApplicationId.HasValue;

        /// Paper IDs to register when creating a new application.
        public List<int> PaperIds { get; set; } = new();
    }

    /// <summary>
    /// Manages student exam applications for a batch/section/semester.
    /// Flow:
    ///  1. Select Course → Batch → Section → Semester → Examination
    ///  2. Click Load Students → grid shows all students with Applied / Not Applied status
    ///  3. Faculty ticks students to apply → Save registers new ExamApplications with papers
    /// EXAM_APPLICATION module lock enforced before every save.
    /// </summary>
    public partial class ExamApplicationViewModel : ObservableObject
    {
        private readonly FacultyDbContext _db;
        private readonly AdminUser _currentUser;

        // ─── Cascading Dropdowns ─────────────────────────────────────────
        [ObservableProperty] private ObservableCollection<Models.Course> _courses = new();
        [ObservableProperty] private ObservableCollection<Models.Batch> _batches = new();
        [ObservableProperty] private ObservableCollection<Models.Section> _sections = new();
        [ObservableProperty] private ObservableCollection<Models.Examination> _examinations = new();
        [ObservableProperty] private ObservableCollection<int> _semesters = new();

        [ObservableProperty] private Models.Course? _selectedCourse;
        [ObservableProperty] private Models.Batch? _selectedBatch;
        [ObservableProperty] private Models.Section? _selectedSection;
        [ObservableProperty] private Models.Examination? _selectedExamination;
        [ObservableProperty] private int? _selectedSemester;

        // ─── Module Lock ─────────────────────────────────────────────────
        [ObservableProperty] private bool _isModuleLocked = false;
        [ObservableProperty] private string _lockMessage = "";
        [ObservableProperty] private bool _canSave = true;

        // ─── Student Grid ────────────────────────────────────────────────
        [ObservableProperty] private ObservableCollection<ExamAppRow> _students = new();

        // ─── Stats ───────────────────────────────────────────────────────
        [ObservableProperty] private int _totalStudents;
        [ObservableProperty] private int _appliedCount;
        [ObservableProperty] private int _notAppliedCount;

        // ─── Status ──────────────────────────────────────────────────────
        [ObservableProperty] private bool _isLoading = false;
        [ObservableProperty] private string _statusMessage = "";
        [ObservableProperty] private string _statusColor = "#4CAF50";
        [ObservableProperty] private bool _hasStudents = false;

        public ExamApplicationViewModel(FacultyDbContext db, AdminUser user)
        {
            _db = db;
            _currentUser = user;
        }

        public async Task InitializeAsync()
        {
            await CheckModuleLockAsync();
            await LoadCoursesAsync();
            await LoadExaminationsAsync();
        }

        // ─── Module Lock ─────────────────────────────────────────────────
        private async Task CheckModuleLockAsync()
        {
            try
            {
                var module = await _db.Modules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.ModuleCode == "EXAM_APPLICATION");

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
                    ? "⚠️  Exam Application is currently LOCKED by the COE. You cannot register new applications."
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
                .ToListAsync();
            Courses = new ObservableCollection<Models.Course>(list);
        }

        private async Task LoadExaminationsAsync()
        {
            var list = await _db.Examinations
                .OrderByDescending(e => e.ExamYear)
                .ThenBy(e => e.ExamCode)
                .ToListAsync();
            Examinations = new ObservableCollection<Models.Examination>(list);
        }

        partial void OnSelectedCourseChanged(Models.Course? value)
        {
            SelectedBatch = null;
            SelectedSection = null;
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
            Batches = new ObservableCollection<Models.Batch>(list);
        }

        private Task LoadSemestersAsync(Models.Course course)
        {
            int total = course.TotalSemesters ?? 8;
            Semesters = new ObservableCollection<int>(Enumerable.Range(1, total));
            return Task.CompletedTask;
        }

        partial void OnSelectedBatchChanged(Models.Batch? value)
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
            Sections = new ObservableCollection<Models.Section>(list);
        }

        // ─── Load Students ───────────────────────────────────────────────
        [RelayCommand]
        private async Task LoadStudents()
        {
            if (SelectedSection == null || SelectedExamination == null || SelectedSemester == null)
            {
                SetStatus("Please select Course, Batch, Section, Semester and Examination.", "#FF9800");
                return;
            }

            IsLoading = true;
            Students.Clear();
            HasStudents = false;
            StatusMessage = "";

            try
            {
                var studentList = await _db.Students
                    .Where(s => s.SectionId == SelectedSection.SectionId &&
                                s.CourseId == SelectedCourse!.CourseId)
                    .OrderBy(s => s.RollNumber)
                    .ToListAsync();

                if (!studentList.Any())
                {
                    SetStatus("No students found for the selected section.", "#FF9800");
                    return;
                }

                // Papers for the chosen semester
                var papers = await _db.PaperSet
                    .Where(p => p.CourseId == SelectedCourse!.CourseId &&
                                p.Semester == SelectedSemester)
                    .ToListAsync();

                var studentIds = studentList.Select(s => s.StudentId).ToList();

                // Existing applications for this examination
                var existingApps = await _db.ExamApplications
                    .Where(a => a.ExaminationId == SelectedExamination.ExaminationId &&
                                studentIds.Contains(a.StudentId))
                    .ToListAsync();

                foreach (var student in studentList)
                {
                    var existing = existingApps
                        .FirstOrDefault(a => a.StudentId == student.StudentId);

                    Students.Add(new ExamAppRow
                    {
                        StudentId = student.StudentId,
                        RegistrationNumber = student.RegistrationNumber ?? "-",
                        RollNumber = student.RollNumber ?? "-",
                        FullName = student.FullName,
                        IsApplied = existing != null,
                        ApprovalStatus = existing?.ApprovalStatus ?? "Not Applied",
                        TotalFees = existing?.TotalFees,
                        ExistingApplicationId = existing?.ExamApplicationId,
                        PaperIds = papers.Select(p => p.PaperId).ToList()
                    });
                }

                HasStudents = true;
                TotalStudents = Students.Count;
                AppliedCount = Students.Count(s => s.IsApplied);
                NotAppliedCount = Students.Count(s => !s.IsApplied);

                SetStatus(
                    $"Loaded {Students.Count} students — {AppliedCount} applied, {NotAppliedCount} not applied.",
                    "#4CAF50");
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

        // ─── Save Applications ───────────────────────────────────────────
        [RelayCommand]
        private async Task SaveApplications()
        {
            await CheckModuleLockAsync();
            if (IsModuleLocked)
            {
                SetStatus("Cannot save — module is locked by COE.", "#F44336");
                return;
            }

            if (!Students.Any())
            {
                SetStatus("No students loaded. Load students first.", "#FF9800");
                return;
            }

            IsLoading = true;
            int registered = 0;

            try
            {
                var now = DateTime.Now;
                var toRegister = Students
                    .Where(r => r.IsApplied && !r.ExistingApplicationId.HasValue)
                    .ToList();

                // Build applications with papers in memory, then save all at once
                var newApps = new List<(Models.ExamApplication App, ExamAppRow Row)>();

                foreach (var row in toRegister)
                {
                    var app = new Models.ExamApplication
                    {
                        StudentId = row.StudentId,
                        ExaminationId = SelectedExamination!.ExaminationId,
                        ApplicationDate = now,
                        ApprovalStatus = "Pending",
                        IsPaid = false
                    };

                    foreach (var paperId in row.PaperIds)
                    {
                        app.ExamApplicationPapers.Add(new Models.ExamApplicationPaper
                        {
                            PaperId = paperId,
                            Semester = SelectedSemester
                        });
                    }

                    _db.ExamApplications.Add(app);
                    newApps.Add((app, row));
                }

                // SESSION_CONTEXT already set at login — TR_ExamApplications_Audit writes to AuditLog
                await _db.SaveChangesAsync();

                // Refresh IDs in the UI rows
                foreach (var (app, row) in newApps)
                {
                    row.ExistingApplicationId = app.ExamApplicationId;
                    row.ApprovalStatus = "Pending";
                    registered++;
                }

                AppliedCount = Students.Count(s => s.IsApplied);
                NotAppliedCount = Students.Count(s => !s.IsApplied);

                SetStatus($"✅  Registered {registered} new application(s).", "#4CAF50");
            }
            catch (Exception ex)
            {
                string msg = ex.InnerException?.Message ?? ex.Message;
                if (msg.Contains("locked", StringComparison.OrdinalIgnoreCase))
                {
                    IsModuleLocked = true;
                    CanSave = false;
                    LockMessage = "⚠️  Module was locked by COE during save. Changes were not applied.";
                }
                SetStatus($"Save failed: {msg}", "#F44336");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ─── Bulk Select Helpers ─────────────────────────────────────────
        [RelayCommand]
        private void SelectAll()
        {
            foreach (var row in Students)
                row.IsApplied = true;
            RefreshStats();
        }

        [RelayCommand]
        private void ClearNew()
        {
            foreach (var row in Students.Where(r => r.ExistingApplicationId == null))
                row.IsApplied = false;
            RefreshStats();
        }

        private void RefreshStats()
        {
            AppliedCount = Students.Count(s => s.IsApplied);
            NotAppliedCount = Students.Count(s => !s.IsApplied);
        }

        private void SetStatus(string msg, string color)
        {
            StatusMessage = msg;
            StatusColor = color;
        }
    }
}
