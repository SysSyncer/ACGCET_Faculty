using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ACGCET_Faculty.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace ACGCET_Faculty.ViewModels.MarkEntry
{
    /// <summary>
    /// Shows THIS faculty's complete audit trail — every INSERT/UPDATE they have made.
    /// Also shows any AnomalyDetectionLog entries linked to this user,
    /// so they can see if any anomaly was flagged against their account.
    /// </summary>
    public partial class MyAuditLogViewModel : ObservableObject
    {
        private readonly FacultyDbContext _db;
        private readonly AdminUser _currentUser;

        [ObservableProperty] private ObservableCollection<AuditLog> _auditEntries = new();
        [ObservableProperty] private ObservableCollection<AnomalyDetectionLog> _anomalyFlags = new();
        [ObservableProperty] private bool _isLoading = false;
        [ObservableProperty] private string _statusMsg = "";
        [ObservableProperty] private bool _hasAnomalies = false;

        // Filter
        [ObservableProperty] private string _filterTable = "All";
        [ObservableProperty] private DateTime _fromDate = DateTime.Today.AddDays(-30);
        [ObservableProperty] private DateTime _toDate = DateTime.Today;

        public MyAuditLogViewModel(FacultyDbContext db, AdminUser user)
        {
            _db = db;
            _currentUser = user;
        }

        public async Task InitializeAsync()
        {
            await LoadAsync();
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            IsLoading = true;
            StatusMsg = "";

            try
            {
                // Audit entries for this faculty
                var query = _db.AuditLogs
                    .Where(l => l.AdminUserId == _currentUser.AdminUserId &&
                                l.ActionDate >= FromDate &&
                                l.ActionDate <= ToDate.AddDays(1));

                if (FilterTable != "All")
                    query = query.Where(l => l.TableName == FilterTable);

                var logs = await query
                    .OrderByDescending(l => l.ActionDate)
                    .ToListAsync();

                AuditEntries = new ObservableCollection<AuditLog>(logs);

                // Anomaly flags for this user
                var flags = await _db.AnomalyDetectionLogs
                    .Where(m => m.SuspiciousUserId == _currentUser.AdminUserId)
                    .OrderByDescending(m => m.DetectionDateTime)
                    .ToListAsync();

                AnomalyFlags = new ObservableCollection<AnomalyDetectionLog>(flags);
                HasAnomalies = AnomalyFlags.Any();

                StatusMsg = $"Showing {AuditEntries.Count} audit entries" +
                            (HasAnomalies ? $" | ⚠️ {AnomalyFlags.Count} anomaly flag(s)" : "");
            }
            catch (Exception ex)
            {
                StatusMsg = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
