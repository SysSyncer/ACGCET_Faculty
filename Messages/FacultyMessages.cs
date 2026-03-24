using ACGCET_Faculty.Models;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace ACGCET_Faculty.Messages
{
    /// <summary>Sent by LoginViewModel after a successful BCrypt password verify.</summary>
    public class LoginSuccessMessage : ValueChangedMessage<AdminUser>
    {
        public LoginSuccessMessage(AdminUser user) : base(user) { }
    }

    /// <summary>Sent by FacultyDashboardViewModel when the user clicks Logout.</summary>
    public class LogoutMessage : ValueChangedMessage<bool>
    {
        public LogoutMessage() : base(true) { }
    }
}
