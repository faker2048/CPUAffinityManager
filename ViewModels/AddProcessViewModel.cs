using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Diagnostics;
using System.IO;

namespace _;

public partial class AddProcessViewModel : ObservableObject
{
    private readonly ProcessAffinityService _processAffinityService;
    private readonly CcdService _ccdService;
    private Window? _window;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ProcessInfo> _filteredProcesses = new();

    [ObservableProperty]
    private ProcessInfo? _selectedProcess;

    [ObservableProperty]
    private KeyValuePair<string, CcdConfig>? _selectedCcd;

    [ObservableProperty]
    private IEnumerable<KeyValuePair<string, CcdConfig>> _availableCcds;

    public AddProcessViewModel(
        ProcessAffinityService processAffinityService,
        CcdService ccdService)
    {
        _processAffinityService = processAffinityService;
        _ccdService = ccdService;
        
        _availableCcds = _ccdService.Ccds;
        if (_ccdService.Ccds.Any())
        {
            _selectedCcd = _ccdService.Ccds.First();
        }
        UpdateFilteredProcesses();
    }

    public void SetWindow(Window window)
    {
        _window = window;
    }

    partial void OnSearchTextChanged(string value)
    {
        UpdateFilteredProcesses();
    }

    private void UpdateFilteredProcesses()
    {
        var processes = Process.GetProcesses()
            .Where(p => !string.IsNullOrEmpty(p.ProcessName))
            .Select(p => new ProcessInfo(p.ProcessName))
            .Where(p => string.IsNullOrEmpty(SearchText) || 
                       p.ProcessName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.ProcessName)
            .DistinctBy(p => p.ProcessName);

        FilteredProcesses = new ObservableCollection<ProcessInfo>(processes);
    }

    [RelayCommand]
    private void Confirm()
    {
        if (SelectedProcess == null)
        {
            MessageBox.Show("请选择一个进程", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (SelectedCcd == null || string.IsNullOrEmpty(SelectedCcd.Value.Key))
        {
            MessageBox.Show("请选择一个CCD组", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (_window != null)
        {
            _window.DialogResult = true;
            _window.Close();
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        if (_window != null)
        {
            _window.DialogResult = false;
            _window.Close();
        }
    }

    [RelayCommand]
    private void AddCcd()
    {
        var dialog = new AddCcdWindow();
        var vm = new AddCcdViewModel(_ccdService, dialog);
        dialog.DataContext = vm;
        var result = dialog.ShowDialog();
        
        // 更新 AvailableCcds
        AvailableCcds = _ccdService.Ccds;
        
        // 如果没有选中的 CCD 组，且有可用的 CCD 组，则选中第一个
        if (SelectedCcd == null && _ccdService.Ccds.Any())
        {
            SelectedCcd = _ccdService.Ccds.First();
        }
    }

    [RelayCommand]
    private void EditConfig()
    {
        try
        {
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CPUAffinityManager",
                "config.toml"
            );

            if (!File.Exists(configPath))
            {
                MessageBox.Show("配置文件不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = configPath,
                UseShellExecute = true,
                Verb = "open"
            };
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开配置文件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public record ProcessInfo(string ProcessName);
} 