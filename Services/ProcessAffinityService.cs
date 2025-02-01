using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace _;

public class ProcessAffinityService
{
    /// <summary>
    /// 获取所有运行中的进程, 返回 (ProcessId, ProcessName) 元组
    /// </summary>
    private IEnumerable<(int ProcessId, string ProcessName)> GetRunningProcesses()
    {
        Console.WriteLine("[ProcessAffinityService] 获取运行中的进程列表");
        return Process.GetProcesses()
            .Where(p => !string.IsNullOrEmpty(p.ProcessName))
            .Select(p => (p.Id, p.ProcessName))
            .OrderBy(p => p.ProcessName);
    }

    /// <summary>
    /// 通过进程名称设置CPU亲和性, 如果 processName 有多个进程, 则对每个进程设置亲和性
    /// </summary>
    /// <param name="processName">进程名称（不含.exe）</param>
    /// <param name="affinityMask">CPU亲和性掩码</param>
    /// <returns>操作结果，包含成功和失败信息</returns>
    public static (bool Success, string Message) SetAffinityByName(string processName, long affinityMask)
    {
        Console.WriteLine($"[ProcessAffinityService] 尝试设置进程亲和性，进程名：{processName}，掩码：{affinityMask:X}");
        try
        {
            var processes = Process.GetProcessesByName(processName);
            if (!processes.Any())
            {
                Console.WriteLine($"[ProcessAffinityService] 设置失败：未找到进程 {processName}");
                return (false, $"未找到进程：{processName}");
            }

            var results = new List<(bool Success, string Message)>();
            foreach (var process in processes)
            {
                results.Add(SetAffinityForProcess(process, affinityMask));
            }

            var successCount = results.Count(r => r.Success);
            if (successCount == processes.Length)
            {
                Console.WriteLine($"[ProcessAffinityService] 成功设置 {successCount} 个进程的CPU亲和性");
                return (true, $"成功设置 {successCount} 个进程的CPU亲和性");
            }

            var errors = results.Where(r => !r.Success).Select(r => r.Message);
            Console.WriteLine($"[ProcessAffinityService] 部分进程设置失败，成功：{successCount}，失败：{processes.Length - successCount}");
            return (false, $"部分进程设置失败。成功：{successCount}，失败：{processes.Length - successCount}\n" +
                           string.Join("\n", errors));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProcessAffinityService] 设置CPU亲和性时发生错误：{ex.Message}");
            return (false, $"设置CPU亲和性时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 通过进程ID设置CPU亲和性
    /// </summary>
    /// <param name="processId">进程ID</param>
    /// <param name="affinityMask">CPU亲和性掩码</param>
    /// <returns>操作结果，包含成功和失败信息</returns>
    public static (bool Success, string Message) SetAffinityById(int processId, long affinityMask)
    {
        Console.WriteLine($"[ProcessAffinityService] 尝试设置进程亲和性，进程ID：{processId}，掩码：{affinityMask:X}");
        try
        {
            var process = Process.GetProcessById(processId);
            return SetAffinityForProcess(process, affinityMask);
        }
        catch (ArgumentException)
        {
            Console.WriteLine($"[ProcessAffinityService] 设置失败：未找到进程ID {processId}");
            return (false, $"未找到进程ID：{processId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProcessAffinityService] 设置CPU亲和性时发生错误：{ex.Message}");
            return (false, $"设置CPU亲和性时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取指定进程的CPU亲和性掩码
    /// </summary>
    /// <param name="processId">进程ID</param>
    /// <returns>CPU亲和性掩码和进程信息</returns>
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
            return "获取失败";
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
                return "进程未运行";
            }

            var affinityMasks = processes.Select(p => p.ProcessorAffinity.ToInt64()).Distinct().ToList();
            if (affinityMasks.Count == 1)
            {
                return FormatAffinityMaskToHumanReadable(affinityMasks[0]);
            }
            else
            {
                return $"多个进程({processes.Length}个)亲和性不一致";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProcessAffinityService] 获取进程亲和性时发生错误：{ex.Message}");
            return "获取失败";
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

    private static string FormatAffinityMaskToHumanReadable(long affinityMask)
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
            return "无核心绑定";
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
}