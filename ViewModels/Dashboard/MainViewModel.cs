using ACGCET_Faculty.Messages;
using ACGCET_Faculty.Models;
using ACGCET_Faculty.ViewModels.MarkEntry;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace ACGCET_Faculty.ViewModels.Dashboard
{
    /// <summary>
    /// Root ViewModel for the main window.
    /// Listens for LoginSuccessMessage → shows Dashboard.
    /// Listens for LogoutMessage → shows Login.
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        private readonly FacultyDbContext _db;
        private readonly LoginViewModel _loginVm;

        [ObservableProperty] private object? _currentView;

        public MainViewModel(FacultyDbContext db, LoginViewModel loginVm)
        {
            _db = db;
            _loginVm = loginVm;

            // Start with Login screen
            CurrentView = _loginVm;

            // Listen for successful login
            WeakReferenceMessenger.Default.Register<LoginSuccessMessage>(this, async (r, msg) =>
            {
                var dashboard = new FacultyDashboardViewModel(_db, msg.Value);
                CurrentView = dashboard;
                await dashboard.InitializeAsync();
            });

            // Listen for logout
            WeakReferenceMessenger.Default.Register<LogoutMessage>(this, (r, msg) =>
            {
                // Reset login form
                _loginVm.Username = "";
                _loginVm.Password = "";
                _loginVm.IsErrorVisible = false;
                CurrentView = _loginVm;
            });
        }
    }
}
