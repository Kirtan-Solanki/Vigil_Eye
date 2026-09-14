using Microsoft.UI.Xaml;
using VigilEye.Dashboard.Views;

namespace VigilEye.Dashboard;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "Vigil Eye - Guardian Dashboard";
        RootFrame.Navigate(typeof(MainPage));
    }
}
