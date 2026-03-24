using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ACGCET_Faculty.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace ACGCET_Faculty.ViewModels.Reports
{
    /// <summary>
    /// A row in the missing-marks report grid.
    /// </summary>
    public class MissingMarkRow
    {
        public string RollNumber { get; set; } = "";
        public string RegistrationNumber { get; set; } = "";
        public string FullName { get; set; } = "";
        public string PaperCode { get; set; } = "";
        public string PaperName { get; set; } = "";
        public string MissingTests { get; set; } = "";
    }

    /// <summary>
    /// Read-only report: which students are missing internal marks
    /// for one or more test types in the selected batch/section/semester.
    /// No module lock needed — this is a read-only diagnostic view.
    /// </summary>
    public partial class MissingMarksViewModel : ObservableObject
    {
        private readonly FacultyDbContext _db;

        // ─── Cascading Dropdowns ─────────────────────────────────────────
        [ObservableProperty] private ObservableCollection<Models.Course> _courses = new();
        [ObservableProperty] private ObservableCollection<Models.Batch> _batches = new();
        [ObservableProperty] private ObservableCollection<Models.Section> _sections = new();
        [ObservableProperty] private ObservableCollection<int> _semesters = new();

        [ObservableProperty] private Models.Course? _selectedCourse;
        [ObservableProperty] private Models.Batch? _selectedBatch;
        [ObservableProperty] private Models.Section? _selectedSection;
        [ObservableProperty] private int? _selectedSemester;

        // ─── Results ─────────────────────────────────────────────────────
        [ObservableProperty] private ObservableCollection<MissingMarkRow> _missingMarks = new();
        [ObservableProperty] private bool _isLoading = false;
        [ObservableProperty] private bool _hasResults = false;
        [ObservableProperty] private int _missingCount;
        [ObservableProperty] private int _totalPaperSlots;
        [ObservableProperty] private string _statusMessage = "";
        [ObservableProperty] private string _statusColor = "#4CAF50";

        public MissingMarksViewModel(FacultyDbContext db)
        {
            _db = db;
        }

        public async Task InitializeAsync()
        {
            await LoadCoursesAsync();
        }

        private async Task LoadCoursesAsync()
        {
            var list = await _db.Courses
                .Include(c => c.Program)
                .ToListAsync();
            Courses = new ObservableCollection<Models.Course>(list);
        }

        partial void OnSelectedCourseChanged(Models.Course? value)
        {
            SelectedBatch = null;
            SelectedSection = null;
            MissingMarks.Clear();
            HasResults = false;
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
            MissingMarks.Clear();
            HasResults = false;
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

        // ─── Generate Report ─────────────────────────────────────────────
        [RelayCommand]
        private async Task GenerateReport()
        {
            if (SelectedSection == null || SelectedSemester == null)
            {
                SetStatus("Please select Course, Batch, Section and Semester.", "#FF9800");
                return;
            }

            IsLoading = true;
            MissingMarks.Clear();
            HasResults = false;
            StatusMessage = "";

            try
            {
                var students = await _db.Students
                    .Where(s => s.SectionId == SelectedSection.SectionId &&
                                s.CourseId == SelectedCourse!.CourseId)
                    .OrderBy(s => s.RollNumber)
                    .ToListAsync();

                var papers = await _db.PaperSet
                    .Where(p => p.CourseId == SelectedCourse!.CourseId &&
                                p.Semester == SelectedSemester)
                    .OrderBy(p => p.PaperCode)
                    .ToListAsync();

                var testTypes = await _db.TestTypes.ToListAsync();

                if (!students.Any())
                {
                    SetStatus("No students found for the selected section.", "#FF9800");
                    return;
                }

                if (!papers.Any())
                {
                    SetStatus("No papers configured for this semester.", "#FF9800");
                    return;
                }

                var studentIds = students.Select(s => s.StudentId).ToList();
                var paperIds = papers.Select(p => p.PaperId).ToList();

                // Fetch all marks in one query
                var existingMarks = await _db.InternalMarks
                    .Where(m => studentIds.Contains(m.StudentId) &&
                                paperIds.Contains(m.PaperId) &&
                                m.Semester == SelectedSemester)
                    .ToListAsync();

                var rows = new List<MissingMarkRow>();
                int totalSlots = students.Count * papers.Count * testTypes.Count;

                foreach (var student in students)
                {
                    foreach (var paper in papers)
                    {
                        var enteredTestTypeIds = existingMarks
                            .Where(m => m.StudentId == student.StudentId &&
                                        m.PaperId == paper.PaperId)
                            .Select(m => m.TestTypeId)
                            .ToHashSet();

                        var missingTestTypes = testTypes
                            .Where(t => !enteredTestTypeIds.Contains(t.TestTypeId))
                            .ToList();

                        if (missingTestTypes.Any())
                        {
                            rows.Add(new MissingMarkRow
                            {
                                RollNumber = student.RollNumber ?? "-",
                                RegistrationNumber = student.RegistrationNumber ?? "-",
                                FullName = student.FullName,
                                PaperCode = paper.PaperCode ?? "-",
                                PaperName = paper.PaperName,
                                MissingTests = string.Join(", ",
                                    missingTestTypes.Select(t => t.TestCode ?? t.TestName))
                            });
                        }
                    }
                }

                MissingMarks = new ObservableCollection<MissingMarkRow>(rows);
                HasResults = true;
                MissingCount = rows.Count;
                TotalPaperSlots = totalSlots;

                if (rows.Any())
                    SetStatus($"⚠️  {rows.Count} missing mark entries found across {students.Count} students.", "#FF9800");
                else
                    SetStatus($"✅  All marks entered for {students.Count} students across {papers.Count} papers.", "#4CAF50");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}", "#F44336");
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
