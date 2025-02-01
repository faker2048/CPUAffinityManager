using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Linq;

public class ProcessMonitorService : IDisposable
{
    private readonly HashSet<Action<int, string>> _processStartedCallbacks = new();
    private readonly HashSet<Action<int>> _processEndedCallbacks = new();
    private readonly Timer _timer;
    private HashSet<int> _currentProcessIds = new();

    public ProcessMonitorService()
    {
        _timer = new Timer(1000); // 每秒检查一次
        _timer.Elapsed += CheckProcesses;
        _currentProcessIds = GetCurrentProcessIds();
        _timer.Start();
    }

    private void CheckProcesses(object? sender, ElapsedEventArgs e)
    {
        var newProcessIds = GetCurrentProcessIds();
        
        // 检查新启动的进程
        var startedProcesses = newProcessIds.Except(_currentProcessIds);
        foreach (var processId in startedProcesses)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                foreach (var callback in _processStartedCallbacks)
                {
                    callback(processId, process.ProcessName);
                }
            }
            catch (ArgumentException)
            {
                // 进程可能已经结束，忽略
            }
        }

        // 检查已结束的进程
        var endedProcesses = _currentProcessIds.Except(newProcessIds);
        foreach (var processId in endedProcesses)
        {
            foreach (var callback in _processEndedCallbacks)
            {
                callback(processId);
            }
        }

        _currentProcessIds = newProcessIds;
    }

    private static HashSet<int> GetCurrentProcessIds()
    {
        return new HashSet<int>(Process.GetProcesses().Select(p => p.Id));
    }

    public void SubscribeToProcessStarted(Action<int, string> callback)
    {
        _processStartedCallbacks.Add(callback);
    }

    public void SubscribeToProcessEnded(Action<int> callback)
    {
        _processEndedCallbacks.Add(callback);
    }

    public void UnsubscribeFromProcessStarted(Action<int, string> callback)
    {
        _processStartedCallbacks.Remove(callback);
    }

    public void UnsubscribeFromProcessEnded(Action<int> callback)
    {
        _processEndedCallbacks.Remove(callback);
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }
} 