using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using _.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace @_.ViewModels;

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
                       p.ProcessName.Contains((string)SearchText, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.ProcessName)
            .DistinctBy(p => p.ProcessName);

        FilteredProcesses = new ObservableCollection<ProcessInfo>(processes);
    }

    [RelayCommand]
    private void Confirm()
    {
        if (SelectedProcess == null)
        {
            MessageBox.Show("Please select a process", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (SelectedCcd == null || string.IsNullOrEmpty(SelectedCcd.Value.Key))
        {
            MessageBox.Show("Please select a CCD group", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
        var vm = new ViewModels.AddCcdViewModel(_ccdService, dialog);
        dialog.DataContext = vm;
        var result = dialog.ShowDialog();
        
        // Update AvailableCcds
        AvailableCcds = _ccdService.Ccds;
        
        // If no CCD group is selected and there are available CCD groups, select the first one
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
                MessageBox.Show("Configuration file does not exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var editorWindow = new ConfigEditorWindow();
            var vm = new ConfigEditorViewModel(editorWindow, configPath);
            editorWindow.DataContext = vm;
            editorWindow.Owner = _window;
            
            var result = editorWindow.ShowDialog();
            if (result == true)
            {
                // Configuration saved, refresh CCD list
                _ccdService.ReloadCcds();
                AvailableCcds = _ccdService.Ccds;
                
                // If no CCD group is selected and there are available CCD groups, select the first one
                if (SelectedCcd == null && _ccdService.Ccds.Any())
                {
                    SelectedCcd = _ccdService.Ccds.First();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open configuration file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public record ProcessInfo(string ProcessName);
} 