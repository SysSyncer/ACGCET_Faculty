using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ACGCET_Faculty.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace ACGCET_Faculty.ViewModels.MarksEntry
{
    /// <summary>
    /// Read-only Marks Review screen.
    /// Lets faculty review entered internal marks for any Course / Batch / Section / Semester.
    /// Actual mark entry is done in InternalMarkEntry.
    /// </summary>
    public partial class MarksEntryViewModel : ObservableObject
    {
        private readonly FacultyDbContext _db;
        private readonly AdminUser? _currentUser;

        // ─── Lock banner ─────────────────────────────────────────────────
        [ObservableProperty] private bool _isModuleLocked = false;
        [ObservableProperty] private string _lockMessage = "";

        // ─── Filters ─────────────────────────────────────────────────────
        [ObservableProperty] private ObservableCollection<Course> _courses = new();
        [ObservableProperty] private ObservableCollection<Batch> _batches = new();
        [ObservableProperty] private ObservableCollection<Section> _sections = new();
        [ObservableProperty] private ObservableCollection<int> _semesters = new();

        [ObservableProperty] private Course? _selectedCourse;
        [ObservableProperty] private Batch? _selectedBatch;
        [ObservableProperty] private Section? _selectedSection;
        [ObservableProperty] private int? _selectedSemester;

        // ─── Results ─────────────────────────────────────────────────────
        [ObservableProperty] private ObservableCollection<InternalMark> _marksList = new();
        [ObservableProperty] private bool _isLoading = false;
        [ObservableProperty] private bool _hasMarks = false;
        [ObservableProperty] private string _statusMessage = "";
        [ObservableProperty] private string _statusColor = "#4CAF50";

        public MarksEntryViewModel(FacultyDbContext db, AdminUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        // For Designer
        public MarksEntryViewModel() { _db = null!; }

        public async Task InitializeAsync()
        {
            if (_db == null) return;
            try
            {
                // Check module lock
                var module = await _db.Modules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.ModuleCode == "INT_MARKS");
                if (module != null)
                {
                    var latestExamId = await _db.Examinations.AsNoTracking()
                        .OrderByDescending(e => e.ExaminationId).Select(e => (int?)e.ExaminationId)
                        .FirstOrDefaultAsync();
                    var locked = await _db.ModuleLocks
                        .AsNoTracking()
                        .AnyAsync(l => l.ModuleId == module.ModuleId && l.IsLocked == true
                            && (l.ExaminationId == null || l.ExaminationId == latestExamId));
                    IsModuleLocked = locked;
                    LockMessage = locked
                        ? "Internal Marks module is currently LOCKED by the COE."
                        : "";
                }

                // Load courses
                var courses = await _db.Courses
                    .Include(c => c.Program)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();
                Courses = new ObservableCollection<Course>(courses);
            }
            catch { /* non-fatal */ }
        }

        // ─── Cascading dropdowns ─────────────────────────────────────────
        partial void OnSelectedCourseChanged(Course? value)
        {
            SelectedBatch = null;
            SelectedSection = null;
            SelectedSemester = null;
            Batches.Clear();
            Sections.Clear();
            Semesters.Clear();
            MarksList.Clear();
            HasMarks = false;
            if (value == null) return;
            _ = LoadBatchesAsync(value.CourseId);
            LoadSemesters(value);
        }

        private async Task LoadBatchesAsync(int courseId)
        {
            var list = await _db.Batches
                .Where(b => b.CourseId == courseId)
                .OrderByDescending(b => b.BatchYear)
                .ToListAsync();
            Batches = new ObservableCollection<Batch>(list);
        }

        private void LoadSemesters(Course course)
        {
            int total = course.TotalSemesters ?? 8;
            Semesters = new ObservableCollection<int>(Enumerable.Range(1, total));
        }

        partial void OnSelectedBatchChanged(Batch? value)
        {
            SelectedSection = null;
            Sections.Clear();
            MarksList.Clear();
            HasMarks = false;
            if (value == null) return;
            _ = LoadSectionsAsync(value.BatchId);
        }

        private async Task LoadSectionsAsync(int batchId)
        {
            var list = await _db.Sections
                .Where(s => s.BatchId == batchId)
                .OrderBy(s => s.SectionName)
                .ToListAsync();
            Sections = new ObservableCollection<Section>(list);
        }

        // ─── Load Marks ──────────────────────────────────────────────────
        [RelayCommand]
        private async Task LoadMarks()
        {
            if (_db == null || SelectedCourse == null) return;

            IsLoading = true;
            MarksList.Clear();
            HasMarks = false;
            StatusMessage = "";

            try
            {
                var query = _db.InternalMarks
                    .Include(m => m.Student)
                    .Include(m => m.Paper)
                    .Include(m => m.TestType)
                    .Where(m => m.Student != null && m.Student.CourseId == SelectedCourse.CourseId);

                if (SelectedBatch != null)
                    query = query.Where(m => m.Student!.BatchId == SelectedBatch.BatchId);

                if (SelectedSection != null)
                    query = query.Where(m => m.Student!.SectionId == SelectedSection.SectionId);

                if (SelectedSemester.HasValue)
                    query = query.Where(m => m.Semester == SelectedSemester.Value);

                var list = await query
                    .OrderBy(m => m.Student!.RollNumber)
                    .ThenBy(m => m.Paper!.PaperCode)
                    .Take(1000)
                    .ToListAsync();

                MarksList = new ObservableCollection<InternalMark>(list);
                HasMarks = MarksList.Any();

                SetStatus(
                    HasMarks
                        ? $"Showing {MarksList.Count} mark record(s)."
                        : "No marks found for the selected filters.",
                    HasMarks ? "#4CAF50" : "#FF9800");
            }
            catch (Exception ex)
            {
                SetStatus($"Error loading marks: {ex.Message}", "#F44336");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SetStatus(string msg, string color)
        {
            StatusMessage = msg;
            StatusColor = color;
        }
    }
}
