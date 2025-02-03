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
        MonitoredProcessListItems = new ObservableCollection<MonitoredProcessListItem>(
            _monitoredProcessService.MonitoredProcesses.Values.Select(p =>
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
                    ProcessAffinityHumanReadable = affinityString
                };
            }));
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
    }

    public void Dispose()
    {
        _updateDebouncer.Dispose();
    }
}