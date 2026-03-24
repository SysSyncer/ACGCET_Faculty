using System;
using System.Threading.Tasks;
using ACGCET_Faculty.Messages;
using ACGCET_Faculty.Models;
using BCrypt.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;

namespace ACGCET_Faculty.ViewModels.Dashboard
{
    /// <summary>
    /// Handles faculty login. On success:
    ///   1. Sets SQL SESSION_CONTEXT so triggers know who is acting
    ///   2. Broadcasts LoginSuccessMessage with the AdminUser object
    /// </summary>
    public partial class LoginViewModel : ObservableObject
    {
        private readonly FacultyDbContext _db;

        public LoginViewModel(FacultyDbContext db)
        {
            _db = db;
        }

        [ObservableProperty] private string _username = "";
        [ObservableProperty] private string _password = "";
        [ObservableProperty] private string _errorMessage = "";
        [ObservableProperty] private bool _isPasswordVisible = false;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotLoading))]
        private bool _isLoading;
        [ObservableProperty] private bool _isErrorVisible;

        public bool IsNotLoading => !IsLoading;

        [RelayCommand]
        private void TogglePasswordVisibility() => IsPasswordVisible = !IsPasswordVisible;

        [RelayCommand]
        private async Task Login()
        {
            if (IsLoading) return;
            IsErrorVisible = false;
            IsLoading = true;

            try
            {
                if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
                {
                    SetError("Please enter username and password.");
                    return;
                }

                var user = await _db.AdminUsers
                    .FirstOrDefaultAsync(u => u.UserName == Username);

                if (user == null)
                {
                    SetError("Invalid username or password.");
                    return;
                }

                if (user.IsLocked == true)
                {
                    SetError("Your account has been locked. Please contact the COE office.");
                    return;
                }

                if (user.IsActive != true)
                {
                    SetError("Your account is inactive. Please contact the administrator.");
                    return;
                }

                // BCrypt verify on background thread — keeps UI responsive
                bool valid = await Task.Run(() =>
                    BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash));

                if (!valid)
                {
                    user.FailedLoginAttempts = (user.FailedLoginAttempts ?? 0) + 1;
                    if (user.FailedLoginAttempts >= 5)
                        user.IsLocked = true;
                    await _db.SaveChangesAsync();
                    SetError(user.IsLocked == true
                        ? "Account locked due to too many failed attempts. Contact the COE office."
                        : "Invalid username or password.");
                    return;
                }

                // Faculty portal is restricted to Staff role only
                var hasAccess = await _db.AdminUserRoles
                    .Include(r => r.Role)
                    .AnyAsync(r => r.AdminUserId == user.AdminUserId &&
                                   r.Role!.RoleName == "Staff");

                if (!hasAccess)
                {
                    SetError("Access denied. This portal is for faculty members only. Please use the Admin portal.");
                    return;
                }

                // ─── SUCCESS ───────────────────────────────────────────────
                // Set SESSION_CONTEXT so SQL triggers capture this username
                _db.SetSessionContext(user.UserName);

                // Update last login time
                user.LastLoginDate = DateTime.Now;
                user.FailedLoginAttempts = 0;
                await _db.SaveChangesAsync();

                // Broadcast to MainViewModel
                WeakReferenceMessenger.Default.Send(new LoginSuccessMessage(user));
            }
            catch (Exception ex)
            {
                SetError($"Login error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SetError(string msg)
        {
            ErrorMessage = msg;
            IsErrorVisible = true;
        }
    }
}
