using System.Diagnostics;

namespace @_.Services;

public class ProcessAffinityService
{
    /// <summary>
    /// Get all running processes, returns (ProcessId, ProcessName) tuples
    /// </summary>
    private IEnumerable<(int ProcessId, string ProcessName)> GetRunningProcesses()
    {
        Console.WriteLine("[ProcessAffinityService] Getting list of running processes");
        return Process.GetProcesses()
            .Where(p => !string.IsNullOrEmpty(p.ProcessName))
            .Select(p => (p.Id, p.ProcessName))
            .OrderBy(p => p.ProcessName);
    }

    /// <summary>
    /// Set CPU affinity by process name, if there are multiple processes with the same name, set affinity for each process
    /// </summary>
    /// <param name="processName">Process name (without .exe)</param>
    /// <param name="affinityMask">CPU affinity mask</param>
    /// <returns>Operation result, including success and failure information</returns>
    public static (bool Success, string Message) SetAffinityByName(string processName, long affinityMask)
    {
        Console.WriteLine($"[ProcessAffinityService] Attempting to set process affinity, process name: {processName}, mask: {affinityMask:X}");
        try
        {
            var processes = Process.GetProcessesByName(processName);
            if (!processes.Any())
            {
                Console.WriteLine($"[ProcessAffinityService] Setting failed: Process not found {processName}");
                return (false, $"Process not found: {processName}");
            }

            var results = new List<(bool Success, string Message)>();
            foreach (var process in processes)
            {
                results.Add(SetAffinityForProcess(process, affinityMask));
            }

            var successCount = results.Count(r => r.Success);
            if (successCount == processes.Length)
            {
                Console.WriteLine($"[ProcessAffinityService] Successfully set CPU affinity for {successCount} processes");
                return (true, $"Successfully set CPU affinity for {successCount} processes");
            }

            var errors = results.Where(r => !r.Success).Select(r => r.Message);
            Console.WriteLine($"[ProcessAffinityService] Some processes failed to set, success: {successCount}, failed: {processes.Length - successCount}");
            return (false, $"Some processes failed to set. Success: {successCount}, Failed: {processes.Length - successCount}\n" +
                           string.Join("\n", errors));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProcessAffinityService] Error occurred while setting CPU affinity: {ex.Message}");
            return (false, $"Error occurred while setting CPU affinity: {ex.Message}");
        }
    }

    /// <summary>
    /// Set CPU affinity by process ID
    /// </summary>
    /// <param name="processId">Process ID</param>
    /// <param name="affinityMask">CPU affinity mask</param>
    /// <returns>Operation result, including success and failure information</returns>
    public static (bool Success, string Message) SetAffinityById(int processId, long affinityMask)
    {
        Console.WriteLine($"[ProcessAffinityService] Attempting to set process affinity, process ID: {processId}, mask: {affinityMask:X}");
        try
        {
            var process = Process.GetProcessById(processId);
            return SetAffinityForProcess(process, affinityMask);
        }
        catch (ArgumentException)
        {
            Console.WriteLine($"[ProcessAffinityService] Setting failed: Process ID {processId} not found");
            return (false, $"Process ID not found: {processId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProcessAffinityService] Error occurred while setting CPU affinity: {ex.Message}");
            return (false, $"Error occurred while setting CPU affinity: {ex.Message}");
        }
    }

    /// <summary>
    /// Get CPU affinity mask for the specified process
    /// </summary>
    /// <param name="processId">Process ID</param>
    /// <returns>CPU affinity mask and process information</returns>
    public static (bool Success, long AffinityMask, string ProcessName, string Message) GetAffinity(int processId)
    {
        Console.WriteLine($"[ProcessAffinityService] 尝试获取进程亲和性，进程ID：{processId}");
        try
        {
            var process = Process.GetProcessById(processId);
            var affinityMask = process.ProcessorAffinity.ToInt64();
            Console.WriteLine($"[ProcessAffinityService] 成功获取进程亲和性，进程：{process.ProcessName}，掩码：{affinityMask:X}");
            return (true, affinityMask, process.ProcessName, "获取成功");
        }
        catch (ArgumentException)
        {
            Console.WriteLine($"[ProcessAffinityService] 获取失败：未找到进程ID {processId}");
            return (false, 0, string.Empty, $"未找到进程ID：{processId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProcessAffinityService] 获取CPU亲和性时发生错误：{ex.Message}");
            return (false, 0, string.Empty, $"获取CPU亲和性时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 将进程的 CPU 亲和性掩码转换为人类可读的格式
    /// </summary>
    /// <param name="processId">目标进程的 ID</param>
    /// <returns>格式化后的 CPU 亲和性字符串，例如："0-7, 16-23"</returns>
    public static string GetProcessAffinityHumanReadable(int processId)
    {
        var (success, affinityMask, processName, message) = GetAffinity(processId);
        if (!success)
        {
            return "Failed to get";
        }
        return FormatAffinityMaskToHumanReadable(affinityMask);
    }

    public static string GetProcessAffinityHumanReadableByName(string processName)
    {
        try
        {
            var processes = Process.GetProcessesByName(processName);
            if (!processes.Any())
            {
                return "Process not running";
            }

            var affinityMasks = processes.Select(p => p.ProcessorAffinity.ToInt64()).Distinct().ToList();
            if (affinityMasks.Count == 1)
            {
                return FormatAffinityMaskToHumanReadable(affinityMasks[0]);
            }
            else
            {
                return $"Multiple processes ({processes.Length}) with inconsistent affinity";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProcessAffinityService] Error occurred while getting process affinity: {ex.Message}");
            return "Failed to get";
        }
    }

    /// <summary>
    /// 为单个进程设置CPU亲和性
    /// </summary>
    private static (bool Success, string Message) SetAffinityForProcess(Process process, long affinityMask)
    {
        Console.WriteLine($"[ProcessAffinityService] 尝试设置进程亲和性，进程：{process.ProcessName}({process.Id})，掩码：{affinityMask:X}");
        try
        {
            process.ProcessorAffinity = new IntPtr(affinityMask);
            Console.WriteLine($"[ProcessAffinityService] 成功设置进程亲和性：{process.ProcessName}({process.Id})");
            return (true, $"成功设置进程 {process.ProcessName}({process.Id}) 的CPU亲和性");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProcessAffinityService] 设置进程亲和性失败：{process.ProcessName}({process.Id})，错误：{ex.Message}");
            return (false, $"进程 {process.ProcessName}({process.Id}): {ex.Message}");
        }
    }

    /// <summary>
    /// 创建CPU亲和性掩码
    /// </summary>
    /// <param name="cores">要启用的CPU核心列表（从0开始）</param>
    /// <returns>CPU亲和性掩码</returns>
    public static long CreateAffinityMask(params int[] cores)
    {
        if (cores == null || cores.Length == 0)
        {
            throw new ArgumentException("必须指定至少一个CPU核心");
        }

        long mask = 0;
        foreach (var core in cores)
        {
            if (core < 0 || core > 63) // 最多支持64个核心
            {
                throw new ArgumentException($"CPU核心编号必须在0-63之间：{core}");
            }

            mask |= (1L << core);
        }

        return mask;
    }

    /// <summary>
    /// 获取已启用的CPU核心列表
    /// </summary>
    /// <param name="affinityMask">CPU亲和性掩码</param>
    /// <returns>已启用的CPU核心列表</returns>
    public static int[] GetEnabledCores(long affinityMask)
    {
        var cores = new List<int>();
        for (int i = 0; i < 64; i++) // 最多检查64个核心
        {
            if ((affinityMask & (1L << i)) != 0)
            {
                cores.Add(i);
            }
        }

        return cores.ToArray();
    }

    public static int GetProcessorCount()
    {
        return Environment.ProcessorCount;
    }

    /// <summary>
    /// Restore all processes to full CPU affinity
    /// </summary>
    /// <returns>Operation result with success/failure counts</returns>
    public static (bool Success, string Message, int SuccessCount, int FailCount) RestoreAllProcessesAffinity()
    {
        Console.WriteLine("[ProcessAffinityService] Starting to restore all processes to full CPU affinity");
        
        var processorCount = GetProcessorCount();
        var allCoresMask = CreateAffinityMask(Enumerable.Range(0, processorCount).ToArray());
        
        var processes = Process.GetProcesses();
        var successCount = 0;
        var failCount = 0;

        foreach (var process in processes)
        {
            try
            {
                var result = SetAffinityById(process.Id, allCoresMask);
                if (result.Success)
                    successCount++;
                else
                    failCount++;
            }
            catch
            {
                failCount++;
            }
        }

        var success = successCount > 0;
        var message = $"Restore completed. Success: {successCount}, Failed: {failCount}";
        Console.WriteLine($"[ProcessAffinityService] {message}");
        
        return (success, message, successCount, failCount);
    }

    /// <summary>
    /// Apply default CCD to processes not in the monitored list
    /// </summary>
    /// <param name="defaultCcdCores">Default CCD core configuration</param>
    /// <param name="excludeProcessNames">Process names to exclude from default CCD application</param>
    /// <returns>Operation result with success/failure counts</returns>
    public static (bool Success, string Message, int ProcessedCount) ApplyDefaultCcdToOtherProcesses(
        int[] defaultCcdCores, 
        HashSet<string> excludeProcessNames)
    {
        Console.WriteLine("[ProcessAffinityService] Applying default CCD to other processes");
        
        if (defaultCcdCores == null || defaultCcdCores.Length == 0)
        {
            return (false, "Default CCD cores not specified", 0);
        }

        var processes = Process.GetProcesses()
            .Where(p => !string.IsNullOrEmpty(p.ProcessName) && !excludeProcessNames.Contains(p.ProcessName))
            .ToList();

        var affinityMask = CreateAffinityMask(defaultCcdCores);
        var processedCount = 0;
        
        foreach (var process in processes)
        {
            try
            {
                var result = SetAffinityById(process.Id, affinityMask);
                if (result.Success)
                {
                    processedCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProcessAffinityService] Failed to set default CCD for process {process.ProcessName}: {ex.Message}");
            }
        }

        var message = $"Applied default CCD to {processedCount} processes";
        Console.WriteLine($"[ProcessAffinityService] {message}");
        
        return (true, message, processedCount);
    }

    public static string FormatAffinityMaskToHumanReadable(long affinityMask)
    {
        var cores = new List<int>();
        for (int i = 0; i < 64; i++)
        {
            if ((affinityMask & (1L << i)) != 0)
            {
                cores.Add(i);
            }
        }

        if (cores.Count == 0)
        {
            return "No cores bound";
        }

        var ranges = new List<string>();
        int start = cores[0];
        int prev = cores[0];

        for (int i = 1; i < cores.Count; i++)
        {
            if (cores[i] != prev + 1)
            {
                ranges.Add(start == prev ? $"{start}" : $"{start}-{prev}");
                start = cores[i];
            }
            prev = cores[i];
        }

        ranges.Add(start == prev ? $"{start}" : $"{start}-{prev}");
        return string.Join(", ", ranges);
    }

    /// <summary>
    /// Format core array to human readable string
    /// </summary>
    /// <param name="cores">Array of core numbers</param>
    /// <returns>Human readable core range string</returns>
    public static string FormatCoreArrayToHumanReadable(int[] cores)
    {
        if (cores == null || cores.Length == 0)
        {
            return "No cores";
        }

        var sortedCores = cores.OrderBy(c => c).ToList();
        var ranges = new List<string>();
        int start = sortedCores[0];
        int prev = sortedCores[0];

        for (int i = 1; i < sortedCores.Count; i++)
        {
            if (sortedCores[i] != prev + 1)
            {
                ranges.Add(start == prev ? $"{start}" : $"{start}-{prev}");
                start = sortedCores[i];
            }
            prev = sortedCores[i];
        }

        ranges.Add(start == prev ? $"{start}" : $"{start}-{prev}");
        return string.Join(", ", ranges);
    }
}