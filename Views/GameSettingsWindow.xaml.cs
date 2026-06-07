using System.Windows;
using _.ViewModels;

namespace _;

/// <summary>
/// Dialog window for configuring automatic game mode (target CCD and game name list).
/// </summary>
public partial class GameSettingsWindow : Window
{
    public GameSettingsWindow(GameSettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
