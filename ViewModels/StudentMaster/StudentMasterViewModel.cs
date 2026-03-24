using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ACGCET_Faculty.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ACGCET_Faculty.ViewModels.StudentMaster
{
    public partial class StudentMasterViewModel : ObservableObject
    {
        private readonly FacultyDbContext _db;

        // ─── Lock ────────────────────────────────────────────────────────
        [ObservableProperty] private bool _isDataEntryLocked = true;
        [ObservableProperty] private string _lockMessage = "";

        // ─── Filters ─────────────────────────────────────────────────────
        [ObservableProperty] private ObservableCollection<Course> _courses = new();
        [ObservableProperty] private ObservableCollection<Batch> _batches = new();
        [ObservableProperty] private ObservableCollection<Section> _sections = new();

        [ObservableProperty] private Course? _selectedCourse;
        [ObservableProperty] private Batch? _selectedBatch;
        [ObservableProperty] private Section? _selectedSection;

        // ─── Students ────────────────────────────────────────────────────
        [ObservableProperty] private ObservableCollection<Student> _students = new();
        [ObservableProperty] private bool _isLoading = false;
        [ObservableProperty] private bool _hasStudents = false;

        public StudentMasterViewModel(FacultyDbContext db)
        {
            _db = db;
        }

        // For Designer
        public StudentMasterViewModel() { _db = null!; }

        public async Task InitializeAsync()
        {
            if (_db == null) return;
            try
            {
                // Check module lock
                var module = await _db.Modules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.ModuleCode == "STU_MASTER");
                if (module != null)
                {
                    var latestExamId = await _db.Examinations.AsNoTracking()
                        .OrderByDescending(e => e.ExaminationId).Select(e => (int?)e.ExaminationId)
                        .FirstOrDefaultAsync();
                    var lockEntry = await _db.ModuleLocks
                        .AsNoTracking()
                        .FirstOrDefaultAsync(l => l.ModuleId == module.ModuleId && l.IsLocked == true
                            && (l.ExaminationId == null || l.ExaminationId == latestExamId));
                    if (lockEntry != null)
                    {
                        IsDataEntryLocked = true;
                        LockMessage = "LOCKED BY COE: " + (lockEntry.LockReason ?? "Student master editing is currently disabled.");
                    }
                    else
                    {
                        IsDataEntryLocked = false;
                        LockMessage = "";
                    }
                }
                else
                {
                    IsDataEntryLocked = false;
                    LockMessage = "";
                }

                // Load courses for filter dropdown
                var courses = await _db.Courses
                    .Include(c => c.Program)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();
                Courses = new ObservableCollection<Course>(courses);
            }
            catch (System.Exception ex)
            {
                LockMessage = "Error initialising: " + ex.Message;
            }
        }

        // ─── Cascading dropdowns ─────────────────────────────────────────
        partial void OnSelectedCourseChanged(Course? value)
        {
            SelectedBatch = null;
            SelectedSection = null;
            Students.Clear();
            HasStudents = false;
            Batches.Clear();
            Sections.Clear();
            if (value == null) return;
            _ = LoadBatchesAsync(value.CourseId);
        }

        private async Task LoadBatchesAsync(int courseId)
        {
            var list = await _db.Batches
                .Where(b => b.CourseId == courseId)
                .OrderByDescending(b => b.BatchYear)
                .ToListAsync();
            Batches = new ObservableCollection<Batch>(list);
        }

        partial void OnSelectedBatchChanged(Batch? value)
        {
            SelectedSection = null;
            Students.Clear();
            HasStudents = false;
            Sections.Clear();
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

        // ─── Load Students ───────────────────────────────────────────────
        [RelayCommand]
        private async Task LoadStudents()
        {
            if (_db == null || SelectedCourse == null) return;

            IsLoading = true;
            Students.Clear();
            HasStudents = false;

            try
            {
                var query = _db.Students
                    .Include(s => s.Course)
                    .Include(s => s.Batch)
                    .Include(s => s.Section)
                    .Where(s => s.CourseId == SelectedCourse.CourseId);

                if (SelectedBatch != null)
                    query = query.Where(s => s.BatchId == SelectedBatch.BatchId);

                if (SelectedSection != null)
                    query = query.Where(s => s.SectionId == SelectedSection.SectionId);

                var list = await query
                    .OrderBy(s => s.RollNumber)
                    .ToListAsync();

                Students = new ObservableCollection<Student>(list);
                HasStudents = Students.Count > 0;
            }
            catch (System.Exception ex)
            {
                LockMessage = "Error loading students: " + ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
