using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ACGCET_Faculty.ViewModels.Dashboard;

namespace ACGCET_Faculty.Views.Dashboard
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is LoginViewModel vm)
                vm.PropertyChanged += ViewModel_PropertyChanged;
        }

        // When switching back to hidden mode, sync the PasswordBox with whatever is in the VM
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LoginViewModel.IsPasswordVisible) &&
                DataContext is LoginViewModel vm && !vm.IsPasswordVisible)
            {
                pbPassword.Password = vm.Password;
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
                vm.Password = ((PasswordBox)sender).Password;
        }
    }
}
