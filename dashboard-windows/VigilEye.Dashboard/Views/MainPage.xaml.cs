using Microsoft.UI.Xaml.Controls;
using VigilEye.Dashboard.ViewModels;

namespace VigilEye.Dashboard.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    public MainPage()
    {
        InitializeComponent();
        ViewModel = new MainViewModel();
    }
}
