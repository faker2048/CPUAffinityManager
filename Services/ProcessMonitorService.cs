using System.Diagnostics;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace @_.Services;

// Combine all process-related information into a record struct
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
        
        // Check for newly started processes
        foreach (var (processKey, processInfo) in newProcesses)
        {
            var key = (processInfo.ProcessId, processInfo.ProcessName);
            if (!_currentProcesses.ContainsKey(key))
            {
                ProcessStarted?.Invoke(processInfo);
            }
        }

        // Check for ended processes
        foreach (var (processKey, processInfo) in _currentProcesses)
        {
            var key = (processInfo.ProcessId, processInfo.ProcessName);
            if (!newProcesses.ContainsKey(key))
            {
                ProcessEnded?.Invoke(processInfo);
            }
        }

        // Check for affinity changes
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
                        ex is Win32Exception ||  // Access denied
                        ex is InvalidOperationException)  // Process has exited
                    {
                        // If we can't get ProcessorAffinity, still return basic process info
                        return new ProcessInfo(p.Id, p.ProcessName);
                    }
                })
                .Where(p => !string.IsNullOrEmpty(p.ProcessName)) // Filter out invalid processes
                .ToDictionary(
                    info => (Id: info.ProcessId, Name: info.ProcessName),
                    info => info,
                    // If there are duplicate keys (rare case), keep the first one
                    new DuplicateKeyComparer<(int Id, string Name)>()
                );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProcessMonitorService] Failed to get process list: {ex.Message}");
            return new Dictionary<(int Id, string Name), ProcessInfo>();
        }
    }

    // Comparer for handling duplicate keys
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