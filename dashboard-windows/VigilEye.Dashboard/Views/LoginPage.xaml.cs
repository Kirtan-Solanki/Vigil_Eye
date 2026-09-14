using Microsoft.UI.Xaml.Controls;
using VigilEye.Dashboard.ViewModels;

namespace VigilEye.Dashboard.Views;

public sealed partial class LoginPage : Page
{
    public LoginViewModel ViewModel { get; }

    public LoginPage()
    {
        InitializeComponent();
        ViewModel = new LoginViewModel();
    }
}
