using System.Windows;
using System.Windows.Controls;
using StoreDesk.Desktop.ViewModels;

namespace StoreDesk.Desktop.Views;

public partial class LoginWindow : Window
{
    public LoginWindow() => InitializeComponent();
    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) { if (DataContext is LoginViewModel vm) vm.Password = PasswordBox.Password; }
}
