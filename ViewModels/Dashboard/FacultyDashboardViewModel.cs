using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ACGCET_Faculty.Messages;
using ACGCET_Faculty.Models;
using ACGCET_Faculty.ViewModels.ExamApplication;
using ACGCET_Faculty.ViewModels.MarkEntry;
using ACGCET_Faculty.ViewModels.MarksEntry;
using ACGCET_Faculty.ViewModels.Reports;
using ACGCET_Faculty.ViewModels.Results;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;

namespace ACGCET_Faculty.ViewModels.Dashboard
{
    /// <summary>
    /// Dashboard shown after login.
    /// Shows:
    ///   - Module open/locked status cards (INT_MARKS, EXT_MARKS, etc.)
    ///   - Last 30 audit entries for THIS faculty
    ///   - Unacknowledged system alerts (anomalies/violations)
    /// </summary>
    public partial class FacultyDashboardViewModel : ObservableObject
    {
        private readonly FacultyDbContext _db;
        private readonly AdminUser _currentUser;

        // ─── Navigation ─────────────────────────────────────────────────
        [ObservableProperty] private object? _currentView;

        // ─── Header ─────────────────────────────────────────────────────
        [ObservableProperty] private string _welcomeMessage = "";
        [ObservableProperty] private string _currentDateTime = "";

        // ─── Module Status ───────────────────────────────────────────────
        [ObservableProperty] private bool _isInternalMarkOpen = true;
        [ObservableProperty] private string _internalMarkStatus = "Checking...";
        [ObservableProperty] private string _internalMarkStatusColor = "#607D8B";

        [ObservableProperty] private bool _isExamAppOpen = true;
        [ObservableProperty] private string _examAppStatus = "Checking...";
        [ObservableProperty] private string _examAppStatusColor = "#607D8B";

        // ─── Recent Audit ─────────────────────────────────────────────────
        [ObservableProperty]
        private ObservableCollection<AuditLog> _myRecentActivity = new();

        // ─── System Alerts ───────────────────────────────────────────────
        [ObservableProperty]
        private ObservableCollection<SystemAlert> _systemAlerts = new();

        [ObservableProperty] private bool _hasAlerts;

        public FacultyDashboardViewModel(FacultyDbContext db, AdminUser user)
        {
            _db = db;
            _currentUser = user;

            WelcomeMessage = $"Welcome, {user.FullName ?? user.UserName}";
            CurrentDateTime = DateTime.Now.ToString("dddd, dd MMM yyyy  hh:mm tt");

            // Start home view
            CurrentView = this;
        }

        public async Task InitializeAsync()
        {
            await LoadDashboardAsync();
        }

        private async Task LoadDashboardAsync()
        {
            // Clear cached entities so lock status reads are always fresh
            _db.ChangeTracker.Clear();

            await LoadModuleStatusAsync();
            await LoadExamAppStatusAsync();
            await LoadMyActivityAsync();
            await LoadAlertsAsync();
        }

        // ─── Module Status ───────────────────────────────────────────────
        private async Task LoadModuleStatusAsync()
        {
            try
            {
                var internalMod = await _db.Modules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.ModuleCode == "INT_MARKS");

                if (internalMod == null)
                {
                    IsInternalMarkOpen = false;
                    InternalMarkStatus = "Module not configured";
                    InternalMarkStatusColor = "#F44336";
                    return;
                }

                // Only check global lock (ExaminationId=null) + latest exam lock
                var latestExamId = await _db.Examinations.AsNoTracking()
                    .OrderByDescending(e => e.ExaminationId).Select(e => (int?)e.ExaminationId)
                    .FirstOrDefaultAsync();
                var locked = await _db.ModuleLocks
                    .AsNoTracking()
                    .AnyAsync(l => l.ModuleId == internalMod.ModuleId && l.IsLocked == true
                        && (l.ExaminationId == null || l.ExaminationId == latestExamId));

                IsInternalMarkOpen = !locked;
                InternalMarkStatus = locked ? "🔒  LOCKED by COE" : "🟢  OPEN for Entry";
                InternalMarkStatusColor = locked ? "#F44336" : "#4CAF50";
            }
            catch
            {
                InternalMarkStatus = "Status unavailable";
                InternalMarkStatusColor = "#607D8B";
            }
        }

        // ─── Exam Application Status ─────────────────────────────────────
        private async Task LoadExamAppStatusAsync()
        {
            try
            {
                var mod = await _db.Modules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.ModuleCode == "EXAM_APPLICATION");

                if (mod == null)
                {
                    IsExamAppOpen = false;
                    ExamAppStatus = "Module not configured";
                    ExamAppStatusColor = "#F44336";
                    return;
                }

                var latestExamId = await _db.Examinations.AsNoTracking()
                    .OrderByDescending(e => e.ExaminationId).Select(e => (int?)e.ExaminationId)
                    .FirstOrDefaultAsync();
                var locked = await _db.ModuleLocks
                    .AsNoTracking()
                    .AnyAsync(l => l.ModuleId == mod.ModuleId && l.IsLocked == true
                        && (l.ExaminationId == null || l.ExaminationId == latestExamId));

                IsExamAppOpen = !locked;
                ExamAppStatus = locked ? "🔒  LOCKED by COE" : "🟢  OPEN for Entry";
                ExamAppStatusColor = locked ? "#F44336" : "#4CAF50";
            }
            catch
            {
                ExamAppStatus = "Status unavailable";
                ExamAppStatusColor = "#607D8B";
            }
        }

        // ─── My Recent Activity ──────────────────────────────────────────
        private async Task LoadMyActivityAsync()
        {
            try
            {
                var logs = await _db.AuditLogs
                    .Where(l => l.AdminUserId == _currentUser.AdminUserId)
                    .OrderByDescending(l => l.ActionDate)
                    .Take(30)
                    .ToListAsync();

                MyRecentActivity = new ObservableCollection<AuditLog>(logs);
            }
            catch { /* non-fatal */ }
        }

        // ─── System Alerts (Anomalies) ───────────────────────────────────
        private async Task LoadAlertsAsync()
        {
            try
            {
                // Show alerts related to this user OR general module alerts
                var alerts = await _db.SystemAlerts
                    .Where(a => a.IsAcknowledged == false &&
                                (a.RelatedUserId == _currentUser.AdminUserId ||
                                 a.AlertType == "ModuleAutoLocked" ||
                                 a.AlertType == "UnlockRequest"))
                    .OrderByDescending(a => a.AlertDateTime)
                    .Take(20)
                    .ToListAsync();

                SystemAlerts = new ObservableCollection<SystemAlert>(alerts);
                HasAlerts = SystemAlerts.Any();
            }
            catch { /* non-fatal */ }
        }

        // ─── Navigation Commands ─────────────────────────────────────────
        [RelayCommand]
        private async Task NavigateTo(string destination)
        {
            switch (destination)
            {
                case "Home":
                    await LoadDashboardAsync();
                    CurrentView = this;
                    break;
                case "InternalMarkEntry":
                    var imVm = new InternalMarkEntryViewModel(_db, _currentUser);
                    CurrentView = imVm;
                    await imVm.InitializeAsync();
                    break;
                case "MyAuditLog":
                    var alVm = new MyAuditLogViewModel(_db, _currentUser);
                    CurrentView = alVm;
                    await alVm.InitializeAsync();
                    break;
                case "StudentMaster":
                    var smVm = new ACGCET_Faculty.ViewModels.StudentMaster.StudentMasterViewModel(_db);
                    CurrentView = smVm;
                    await smVm.InitializeAsync();
                    break;
                case "MarksEntry":
                    var meVm = new MarksEntryViewModel(_db, _currentUser);
                    CurrentView = meVm;
                    await meVm.InitializeAsync();
                    break;
                case "Results":
                    var rVm = new ResultsViewModel(_db, _currentUser);
                    CurrentView = rVm;
                    await rVm.InitializeAsync();
                    break;
                case "ExamApplication":
                    var eaVm = new ExamApplicationViewModel(_db, _currentUser);
                    CurrentView = eaVm;
                    await eaVm.InitializeAsync();
                    break;
                case "MissingMarks":
                    var mmVm = new MissingMarksViewModel(_db);
                    CurrentView = mmVm;
                    await mmVm.InitializeAsync();
                    break;
            }
        }

        [RelayCommand]
        private void Logout()
        {
            WeakReferenceMessenger.Default.Send(new LogoutMessage());
        }

        [RelayCommand]
        private async Task Refresh()
        {
            await LoadDashboardAsync();
        }
    }
}
