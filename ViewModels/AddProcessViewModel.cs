using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Diagnostics;

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
    private KeyValuePair<string, CcdConfig> _selectedCcd;

    public IEnumerable<KeyValuePair<string, CcdConfig>> AvailableCcds => _ccdService.Ccds;

    public AddProcessViewModel(
        ProcessAffinityService processAffinityService,
        CcdService ccdService)
    {
        _processAffinityService = processAffinityService;
        _ccdService = ccdService;

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

        if (SelectedCcd.Key == null)
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

    public record ProcessInfo(string ProcessName);
} 