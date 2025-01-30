using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace _;

public partial class MainViewModel : ObservableObject
{
    private readonly IProcessAffinityService _processService;

    [ObservableProperty]
    private string _processNameInput = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<MonitoredProcess> _monitoredProcesses = new();

    public MainViewModel(IProcessAffinityService processService)
    {
        _processService = processService;
    }

    [RelayCommand]
    private void SetProcessAffinity()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ProcessNameInput))
            {
                StatusMessage = "请输入进程名称";
                return;
            }

            // 设置亲和性掩码 (0-15)
            var cores = Enumerable.Range(0, 16).ToArray();
            long affinityMask = ProcessAffinityService.CreateAffinityMask(cores);

            var result = _processService.SetAffinityByName(ProcessNameInput, affinityMask);
            StatusMessage = result.Message;

            if (result.Success)
            {
                var enabledCores = ProcessAffinityService.GetEnabledCores(affinityMask);
                StatusMessage += $"\n已启用的CPU核心：{string.Join(", ", enabledCores)}";

                // 添加到监控列表
                var processes = _processService.GetRunningProcesses()
                    .Where(p => p.ProcessName.Equals(ProcessNameInput, StringComparison.OrdinalIgnoreCase));

                foreach (var process in processes)
                {
                    var monitoredProcess = new MonitoredProcess
                    {
                        ProcessId = process.ProcessId,
                        ProcessName = process.ProcessName,
                        EnabledCores = string.Join(", ", enabledCores)
                    };

                    if (!MonitoredProcesses.Any(p => p.ProcessId == process.ProcessId))
                    {
                        MonitoredProcesses.Add(monitoredProcess);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"操作失败：{ex.Message}";
        }
    }

    [RelayCommand]
    private void CheckProcessAffinity()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ProcessNameInput))
            {
                StatusMessage = "请输入进程名称";
                return;
            }

            var processes = _processService.GetRunningProcesses()
                .Where(p => p.ProcessName.Contains(ProcessNameInput, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!processes.Any())
            {
                StatusMessage = $"未找到包含 '{ProcessNameInput}' 的进程";
                return;
            }

            var results = new List<string>();
            foreach (var process in processes)
            {
                var affinity = _processService.GetAffinity(process.ProcessId);
                if (affinity.Success)
                {
                    var cores = ProcessAffinityService.GetEnabledCores(affinity.AffinityMask);
                    results.Add($"进程 {process.ProcessName}({process.ProcessId}) 使用的CPU核心：{string.Join(", ", cores)}");
                }
                else
                {
                    results.Add($"进程 {process.ProcessName}({process.ProcessId}): {affinity.Message}");
                }
            }

            StatusMessage = string.Join("\n", results);
        }
        catch (Exception ex)
        {
            StatusMessage = $"操作失败：{ex.Message}";
        }
    }

    [RelayCommand]
    private void RefreshMonitoredProcesses()
    {
        try
        {
            var updatedProcesses = new List<MonitoredProcess>();

            foreach (var process in MonitoredProcesses.ToList())
            {
                var affinity = _processService.GetAffinity(process.ProcessId);
                if (affinity.Success)
                {
                    var cores = ProcessAffinityService.GetEnabledCores(affinity.AffinityMask);
                    process.EnabledCores = string.Join(", ", cores);
                }
                else
                {
                    MonitoredProcesses.Remove(process);
                }
            }

            StatusMessage = "监控列表已刷新";
        }
        catch (Exception ex)
        {
            StatusMessage = $"刷新失败：{ex.Message}";
        }
    }
} 