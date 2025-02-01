using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace _;

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
            MessageBox.Show($"读取配置文件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            _window.Close();
        }
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            File.WriteAllText(_configPath, ConfigContent);
            _window.DialogResult = true;
            _window.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存配置文件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _window.DialogResult = false;
        _window.Close();
    }
} 