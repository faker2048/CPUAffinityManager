using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Diagnostics;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;

namespace _;

public partial class MonitoredProcessListItem : ObservableObject
{
    [ObservableProperty]
    private string _processName = string.Empty;

    [ObservableProperty]
    private string _ccdName = string.Empty;

    [ObservableProperty]
    private int[] _runningPiDs = Array.Empty<int>();
}

public partial class MainViewModel : ObservableObject
{
    private readonly ProcessAffinityService _processAffinityService;
    private readonly MonitoredProcessService _monitoredProcessService;
    private readonly CcdService _ccdService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ObservableCollection<MonitoredProcessListItem> _monitoredProcessListItems = new();

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

        UpdateMonitoredProcesses();
    }

    private void UpdateMonitoredProcesses()
    {
        MonitoredProcessListItems = new ObservableCollection<MonitoredProcessListItem>(
            _monitoredProcessService.MonitoredProcesses.Values.Select(p => new MonitoredProcessListItem
            {
                ProcessName = p.ProcessName,
                CcdName = p.CcdName,
                RunningPiDs = Array.Empty<int>()
            }));
    }

    [RelayCommand]
    private void AddProcess()
    {
        var vm = _serviceProvider.GetRequiredService<AddProcessViewModel>();
        var dialog = new AddProcessWindow(vm);

        var result = dialog.ShowDialog();
        Console.WriteLine($"[MainViewModel] 对话框结果：{result}, SelectedProcess: {vm.SelectedProcess?.ProcessName}, SelectedCcd: {vm.SelectedCcd}");
        if (result == true && vm.SelectedProcess != null && vm.SelectedCcd.Value != null)
        {
            Console.WriteLine($"[MainViewModel] 开始添加进程：{vm.SelectedProcess.ProcessName}，CCD组：{vm.SelectedCcd.Key}");
            var process = new MonitoredProcess
            {
                ProcessName = vm.SelectedProcess.ProcessName,
                CcdName = vm.SelectedCcd.Key
            };
            _monitoredProcessService.AddMonitoredProcess(process);
            UpdateMonitoredProcesses();
        }
        else
        {
            Console.WriteLine("[MainViewModel] 添加进程取消或数据无效");
        }
    }

    [RelayCommand]
    private void RemoveProcess(string processName)
    {
        if (MessageBox.Show(
            $"确定要删除进程 {processName} 吗？",
            "确认删除",
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
            var runningPiDs = item.RunningPiDs;

            if (!_ccdService.Ccds.TryGetValue(ccdName, out var ccd))
            {
                Console.WriteLine($"[MainViewModel] CCD组 {ccdName} 不存在");
                continue;
            }

            var affinityMask = ProcessAffinityService.CreateAffinityMask(ccd.Cores);
            _processAffinityService.SetAffinityByName(processName, affinityMask);
        }

    }
}