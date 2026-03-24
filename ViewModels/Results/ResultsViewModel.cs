using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ACGCET_Faculty.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace ACGCET_Faculty.ViewModels.Results
{
    public partial class ResultsViewModel : ObservableObject
    {
        private readonly FacultyDbContext _db;
        private readonly AdminUser? _currentUser;

        // ─── Filters ─────────────────────────────────────────────────────
        [ObservableProperty] private ObservableCollection<Examination> _examinations = new();
        [ObservableProperty] private ObservableCollection<Models.Course> _courses = new();
        [ObservableProperty] private Examination? _selectedExamination;
        [ObservableProperty] private Models.Course? _selectedCourse;

        // ─── Results ─────────────────────────────────────────────────────
        [ObservableProperty] private ObservableCollection<ExamResult> _resultsList = new();
        [ObservableProperty] private bool _isLoading = false;
        [ObservableProperty] private bool _hasResults = false;
        [ObservableProperty] private string _statusMessage = "";
        [ObservableProperty] private string _statusColor = "#4CAF50";

        // ─── Correction Request ──────────────────────────────────────────
        [ObservableProperty] private ExamResult? _selectedResult;
        [ObservableProperty] private string _correctionReason = "";
        [ObservableProperty] private string _proposedValue = "";
        [ObservableProperty] private bool _isCorrectionPanelVisible = false;

        public ResultsViewModel(FacultyDbContext db, AdminUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        // For Designer
        public ResultsViewModel() { _db = null!; }

        public async Task InitializeAsync()
        {
            if (_db == null) return;
            try
            {
                var exams = await _db.Examinations
                    .Where(e => e.IsResultLocked == true)
                    .OrderByDescending(e => e.ExamYear)
                    .ToListAsync();
                Examinations = new ObservableCollection<Examination>(exams);

                var courses = await _db.Courses
                    .Include(c => c.Program)
                    .ToListAsync();
                Courses = new ObservableCollection<Models.Course>(courses);
            }
            catch { /* non-fatal — dropdowns stay empty */ }
        }

        [RelayCommand]
        private async Task LoadResults()
        {
            if (_db == null) return;
            IsLoading = true;
            ResultsList.Clear();
            HasResults = false;
            StatusMessage = "";

            try
            {
                var query = _db.ExamResults
                    .Include(r => r.Student)
                    .Include(r => r.Paper)
                    .Include(r => r.Examination)
                    .Include(r => r.ResultStatus)
                    .Where(r => r.Examination != null && r.Examination.IsResultLocked == true);

                if (SelectedExamination != null)
                    query = query.Where(r => r.ExaminationId == SelectedExamination.ExaminationId);

                if (SelectedCourse != null)
                    query = query.Where(r => r.Paper!.CourseId == SelectedCourse.CourseId);

                var results = await query
                    .OrderBy(r => r.Student!.FullName)
                    .Take(500)
                    .ToListAsync();

                ResultsList = new ObservableCollection<ExamResult>(results);
                HasResults = ResultsList.Any();

                SetStatus(
                    HasResults
                        ? $"Showing {ResultsList.Count} published result(s)."
                        : "No published results found for the selected filters.",
                    HasResults ? "#4CAF50" : "#FF9800");
            }
            catch (Exception ex)
            {
                SetStatus($"Error loading results: {ex.Message}", "#F44336");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void RequestCorrection()
        {
            if (SelectedResult == null)
            {
                SetStatus("Please select a result row first.", "#FF9800");
                return;
            }
            CorrectionReason = "";
            ProposedValue = "";
            IsCorrectionPanelVisible = true;
        }

        [RelayCommand]
        private void CancelCorrection()
        {
            IsCorrectionPanelVisible = false;
            CorrectionReason = "";
            ProposedValue = "";
        }

        [RelayCommand]
        private async Task SubmitCorrection()
        {
            if (SelectedResult == null || string.IsNullOrWhiteSpace(CorrectionReason))
            {
                SetStatus("Please select a result and provide a reason.", "#FF9800");
                return;
            }

            try
            {
                // Find or use the "Result Correction" request type
                var corrType = await _db.CorrectionRequestTypes
                    .FirstOrDefaultAsync(t => t.TypeCode == "RESULT_CORRECTION" && t.IsActive == true);

                var currentValue = $"Internal: {SelectedResult.InternalTotal}, External: {SelectedResult.ExternalTotal}, " +
                                   $"Total: {SelectedResult.GrandTotal}, Grade: {SelectedResult.Grade}";

                var request = new DataCorrectionRequest
                {
                    CorrectionRequestTypeId = corrType?.CorrectionRequestTypeId,
                    RequestedBy = _currentUser?.AdminUserId,
                    TargetTable = "ExamResults",
                    TargetRecordId = SelectedResult.ExamResultId.ToString(),
                    TargetRecordDetails = $"Student: {SelectedResult.Student?.RegistrationNumber}, Paper: {SelectedResult.Paper?.PaperCode}",
                    CurrentValue = currentValue,
                    ProposedValue = string.IsNullOrWhiteSpace(ProposedValue) ? null : ProposedValue,
                    Reason = CorrectionReason,
                    RequestedDateTime = DateTime.Now,
                    ApprovalStatus = "Pending",
                    IsActive = true
                };

                _db.DataCorrectionRequests.Add(request);
                await _db.SaveChangesAsync();

                IsCorrectionPanelVisible = false;
                CorrectionReason = "";
                ProposedValue = "";
                SetStatus("Correction request submitted successfully. The COE will review it.", "#4CAF50");
            }
            catch (Exception ex)
            {
                SetStatus($"Error submitting correction request: {ex.Message}", "#F44336");
            }
        }

        private void SetStatus(string msg, string color)
        {
            StatusMessage = msg;
            StatusColor = color;
        }
    }
}
