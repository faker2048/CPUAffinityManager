using System.Collections.ObjectModel;
using System.Windows;
using _.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using _.Utils;

namespace @_.ViewModels;

public partial class MonitoredProcessListItem : ObservableObject
{
    [ObservableProperty]
    private string _processName = string.Empty;

    [ObservableProperty]
    private string _ccdName = string.Empty;

    [ObservableProperty]
    private string _processAffinityHumanReadable = string.Empty;

    [ObservableProperty]
    private bool _isDefaultProcess = false;

    public string DisplayName => IsDefaultProcess ? "Default (Other Processes)" : ProcessName;
}

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ProcessAffinityService _processAffinityService;
    private readonly MonitoredProcessService _monitoredProcessService;
    private readonly CcdService _ccdService;
    private readonly IServiceProvider _serviceProvider;
    private readonly Debouncer _updateDebouncer;

    [ObservableProperty]
    private ObservableCollection<MonitoredProcessListItem> _monitoredProcessListItems = new();

    [ObservableProperty]
    private bool _isAutoApplyRules = false;

    public MainViewModel(
        MonitoredProcessService monitoredProcessService,
        ProcessAffinityService processAffinityService,
        CcdService ccdService,
        IServiceProvider serviceProvider)
    {
        _monitoredProcessService = monitoredProcessService;
        _processAffinityService = processAffinityService;
        _ccdService = ccdService;
        _serviceProvider = serviceProvider;

        _isAutoApplyRules = _monitoredProcessService.IsAutoApplyRules;

        this.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(IsAutoApplyRules))
            {
                _monitoredProcessService.IsAutoApplyRules = IsAutoApplyRules;
            }
        };

        _updateDebouncer = new Debouncer(DoUpdateMonitoredProcesses, 1000);

        _monitoredProcessService.MonitoredProcessStarted += (processInfo) =>
        {
            Console.WriteLine($"[MainViewModel] Process started: {processInfo.ProcessName}");
            UpdateMonitoredProcesses();
        };

        _monitoredProcessService.MonitoredProcessEnded += (processInfo) => 
        {
            Console.WriteLine($"[MainViewModel] Process ended: {processInfo.ProcessName}");
            UpdateMonitoredProcesses();
        };

        _monitoredProcessService.MonitoredProcessAffinityChanged += (processInfo) =>
        {
            Console.WriteLine($"[MainViewModel] Process CPU affinity changed: {processInfo.ProcessName}");
            UpdateMonitoredProcesses();
        };

        UpdateMonitoredProcesses();
    }

    private void UpdateMonitoredProcesses()
    {
        _updateDebouncer.Debounce();
    }

    private void DoUpdateMonitoredProcesses()
    {
        var items = new List<MonitoredProcessListItem>();

        // Add regular monitored processes
        items.AddRange(_monitoredProcessService.MonitoredProcesses.Values.Select(p =>
        {
            var ccdConfig = _ccdService.Ccds.GetValueOrDefault(p.CcdName);
            string affinityString;
            if (ccdConfig == null)
            {
                affinityString = "Not set";
            }
            else
            {
                affinityString = ProcessAffinityService.GetProcessAffinityHumanReadableByName(p.ProcessName);
            }

            return new MonitoredProcessListItem
            {
                ProcessName = p.ProcessName,
                CcdName = p.CcdName,
                ProcessAffinityHumanReadable = affinityString,
                IsDefaultProcess = false
            };
        }));

        // Add default process item
        var defaultCcd = _ccdService.DefaultCcd ?? "Not set";
        var defaultAffinityString = "Not set";
        
        if (!string.IsNullOrEmpty(_ccdService.DefaultCcd) && 
            _ccdService.Ccds.TryGetValue(_ccdService.DefaultCcd, out var defaultCcdConfig))
        {
            defaultAffinityString = ProcessAffinityService.FormatCoreArrayToHumanReadable(defaultCcdConfig.Cores);
        }

        items.Add(new MonitoredProcessListItem
        {
            ProcessName = "Default",
            CcdName = defaultCcd,
            ProcessAffinityHumanReadable = defaultAffinityString,
            IsDefaultProcess = true
        });

        MonitoredProcessListItems = new ObservableCollection<MonitoredProcessListItem>(items);
    }

    [RelayCommand]
    private void AddProcess()
    {
        var vm = _serviceProvider.GetRequiredService<AddProcessViewModel>();
        var dialog = new AddProcessWindow(vm);

        var result = dialog.ShowDialog();
        Console.WriteLine($"[MainViewModel] Dialog result: {result}, SelectedProcess: {vm.SelectedProcess?.ProcessName}, SelectedCcd: {vm.SelectedCcd}");
        if (result == true && vm.SelectedProcess != null && vm.SelectedCcd != null)
        {
            Console.WriteLine($"[MainViewModel] Starting to add process: {vm.SelectedProcess.ProcessName}, CCD group: {vm.SelectedCcd.Value.Key}");
            var process = new MonitoredProcess
            {
                ProcessName = vm.SelectedProcess.ProcessName,
                CcdName = vm.SelectedCcd.Value.Key
            };
            _monitoredProcessService.AddMonitoredProcess(process);
            UpdateMonitoredProcesses();
        }
        else
        {
            Console.WriteLine("[MainViewModel] Process addition cancelled or data invalid");
        }
    }

    [RelayCommand]
    private void RemoveProcess(string processName)
    {
        // Handle default process item differently
        if (processName == "Default")
        {
            EditDefaultCcd();
            return;
        }

        if (MessageBox.Show(
            $"Are you sure you want to delete process {processName}?",
            "Confirm Deletion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _monitoredProcessService.RemoveMonitoredProcess(processName);
            UpdateMonitoredProcesses();
        }
    }

    private void EditDefaultCcd()
    {
        var vm = _serviceProvider.GetRequiredService<DefaultCcdViewModel>();
        var dialog = new DefaultCcdWindow(vm);
        
        if (dialog.ShowDialog() == true && vm.SelectedCcd != null)
        {
            var selectedCcd = vm.SelectedCcd == "Not set" ? null : vm.SelectedCcd;
            _ccdService.SetDefaultCcd(selectedCcd);
            UpdateMonitoredProcesses();
        }
    }

    [RelayCommand]
    private void ApplyCcdRules()
    {
        foreach (var item in MonitoredProcessListItems)
        {
            var ccdName = item.CcdName;
            var processName = item.ProcessName;

            if (!_ccdService.Ccds.TryGetValue(ccdName, out var ccd))
            {
                Console.WriteLine($"[MainViewModel] CCD group {ccdName} does not exist");
                continue;
            }

            var affinityMask = ProcessAffinityService.CreateAffinityMask(ccd.Cores);
            var result = ProcessAffinityService.SetAffinityByName(processName, affinityMask);
            if (!result.Success)
            {
                Console.WriteLine($"[MainViewModel] Failed to set CPU affinity: {result.Message}");
            }
        }

        ApplyDefaultCcdToOtherProcesses();
    }

    [RelayCommand]
    private void RestoreAllAffinity()
    {
        if (MessageBox.Show(
            "Are you sure you want to restore all processes to full CPU affinity (0-31)?",
            "Confirm Restore",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        var result = ProcessAffinityService.RestoreAllProcessesAffinity();
        MessageBox.Show(result.Message, "Restore Result", 
                       MessageBoxButton.OK, MessageBoxImage.Information);
        UpdateMonitoredProcesses();
    }


    private void ApplyDefaultCcdToOtherProcesses()
    {
        if (string.IsNullOrEmpty(_ccdService.DefaultCcd) || 
            !_ccdService.Ccds.TryGetValue(_ccdService.DefaultCcd, out var defaultCcd))
        {
            return;
        }

        var monitoredProcessNames = MonitoredProcessListItems
            .Where(item => !item.IsDefaultProcess)
            .Select(item => item.ProcessName)
            .ToHashSet();

        ProcessAffinityService.ApplyDefaultCcdToOtherProcesses(defaultCcd.Cores, monitoredProcessNames);
    }

    public void Dispose()
    {
        _updateDebouncer.Dispose();
    }
}