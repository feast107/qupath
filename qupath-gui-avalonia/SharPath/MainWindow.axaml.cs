using Avalonia.Controls;
using SharPath.ViewModels;

namespace SharPath;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}