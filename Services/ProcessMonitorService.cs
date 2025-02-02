using System.Diagnostics;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace @_.Services;

// 合并所有进程相关的信息到一个记录结构体
public readonly record struct ProcessInfo(int ProcessId, string ProcessName, long AffinityMask = 0)
{
    public override string ToString() => $"{ProcessName}({ProcessId})";
}

public class ProcessMonitorService : IDisposable
{
    public event Action<ProcessInfo>? ProcessStarted;
    public event Action<ProcessInfo>? ProcessEnded;
    public event Action<ProcessInfo>? ProcessAffinityChanged;

    private readonly Timer _timer;
    private Dictionary<(int Id, string Name), ProcessInfo> _currentProcesses;

    public ProcessMonitorService()
    {
        _timer = new Timer(1000);
        _timer.Elapsed += CheckProcesses;
        _currentProcesses = GetCurrentProcesses();
        _timer.Start();
    }

    private void CheckProcesses(object? sender, ElapsedEventArgs e)
    {
        var newProcesses = GetCurrentProcesses();
        
        // 检查新启动的进程
        foreach (var (processKey, processInfo) in newProcesses)
        {
            var key = (processInfo.ProcessId, processInfo.ProcessName);
            if (!_currentProcesses.ContainsKey(key))
            {
                ProcessStarted?.Invoke(processInfo);
            }
        }

        // 检查已结束的进程
        foreach (var (processKey, processInfo) in _currentProcesses)
        {
            var key = (processInfo.ProcessId, processInfo.ProcessName);
            if (!newProcesses.ContainsKey(key))
            {
                ProcessEnded?.Invoke(processInfo);
            }
        }

        // 检查亲和性变化
        foreach (var (processKey, newProcess) in newProcesses)
        {
            var key = (newProcess.ProcessId, newProcess.ProcessName);
            if (_currentProcesses.TryGetValue(key, out var oldProcess) && 
                oldProcess.AffinityMask != newProcess.AffinityMask)
            {
                ProcessAffinityChanged?.Invoke(newProcess);
            }
        }

        _currentProcesses = newProcesses;
    }

    private static Dictionary<(int Id, string Name), ProcessInfo> GetCurrentProcesses()
    {
        try
        {
            return Process.GetProcesses()
                .Select(p =>
                {
                    try
                    {
                        return new ProcessInfo(p.Id, p.ProcessName, (long)p.ProcessorAffinity);
                    }
                    catch (Exception ex) when (
                        ex is Win32Exception ||  // 访问被拒绝
                        ex is InvalidOperationException)  // 进程已退出
                    {
                        // 如果无法获取ProcessorAffinity，仍然返回基本进程信息
                        return new ProcessInfo(p.Id, p.ProcessName);
                    }
                })
                .Where(p => !string.IsNullOrEmpty(p.ProcessName)) // 过滤掉无效的进程
                .ToDictionary(
                    info => (Id: info.ProcessId, Name: info.ProcessName),
                    info => info,
                    // 如果有重复的键（极少情况），保留第一个
                    new DuplicateKeyComparer<(int Id, string Name)>()
                );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProcessMonitorService] 获取进程列表失败：{ex.Message}");
            return new Dictionary<(int Id, string Name), ProcessInfo>();
        }
    }

    // 处理重复键的比较器
    private class DuplicateKeyComparer<TKey> : IEqualityComparer<TKey>
    {
        public bool Equals(TKey? x, TKey? y) => x?.Equals(y) ?? y == null;
        public int GetHashCode(TKey obj) => obj?.GetHashCode() ?? 0;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        
        ProcessStarted = null;
        ProcessEnded = null;
        ProcessAffinityChanged = null;
    }
}