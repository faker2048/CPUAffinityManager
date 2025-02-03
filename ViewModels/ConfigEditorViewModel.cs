using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace @_.ViewModels;

public partial class ConfigEditorViewModel : ObservableObject
{
    private readonly Window _window;
    private readonly string _configPath;

    [ObservableProperty]
    private string _configContent = string.Empty;

    public ConfigEditorViewModel(Window window, string configPath)
    {
        _window = window;
        _configPath = configPath;
        LoadConfig();
    }

    private void LoadConfig()
    {
        try
        {
            ConfigContent = File.ReadAllText(_configPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to read configuration file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            _window.Close();
        }
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            File.WriteAllText(_configPath, (string?)ConfigContent);
            _window.DialogResult = true;
            _window.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save configuration file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _window.DialogResult = false;
        _window.Close();
    }
} 