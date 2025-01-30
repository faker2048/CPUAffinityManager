using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace _;

public interface IProcessAffinityService
{
    (bool Success, string Message) SetAffinityByName(string processName, long affinityMask);
    (bool Success, string Message) SetAffinityById(int processId, long affinityMask);
    (bool Success, long AffinityMask, string ProcessName, string Message) GetAffinity(int processId);
    IEnumerable<(int ProcessId, string ProcessName)> GetRunningProcesses();
}

public class ProcessAffinityService : IProcessAffinityService
{
    /// <summary>
    /// 获取所有运行中的进程
    /// </summary>
    public IEnumerable<(int ProcessId, string ProcessName)> GetRunningProcesses()
    {
        return Process.GetProcesses()
            .Where(p => !string.IsNullOrEmpty(p.ProcessName))
            .Select(p => (p.Id, p.ProcessName))
            .OrderBy(p => p.ProcessName);
    }

    /// <summary>
    /// 通过进程名称设置CPU亲和性
    /// </summary>
    /// <param name="processName">进程名称（不含.exe）</param>
    /// <param name="affinityMask">CPU亲和性掩码</param>
    /// <returns>操作结果，包含成功和失败信息</returns>
    public (bool Success, string Message) SetAffinityByName(string processName, long affinityMask)
    {
        try
        {
            var processes = Process.GetProcessesByName(processName);
            if (!processes.Any())
            {
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
                return (true, $"成功设置 {successCount} 个进程的CPU亲和性");
            }

            var errors = results.Where(r => !r.Success).Select(r => r.Message);
            return (false, $"部分进程设置失败。成功：{successCount}，失败：{processes.Length - successCount}\n" +
                          string.Join("\n", errors));
        }
        catch (Exception ex)
        {
            return (false, $"设置CPU亲和性时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 通过进程ID设置CPU亲和性
    /// </summary>
    /// <param name="processId">进程ID</param>
    /// <param name="affinityMask">CPU亲和性掩码</param>
    /// <returns>操作结果，包含成功和失败信息</returns>
    public (bool Success, string Message) SetAffinityById(int processId, long affinityMask)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            return SetAffinityForProcess(process, affinityMask);
        }
        catch (ArgumentException)
        {
            return (false, $"未找到进程ID：{processId}");
        }
        catch (Exception ex)
        {
            return (false, $"设置CPU亲和性时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取指定进程的CPU亲和性掩码
    /// </summary>
    /// <param name="processId">进程ID</param>
    /// <returns>CPU亲和性掩码和进程信息</returns>
    public (bool Success, long AffinityMask, string ProcessName, string Message) GetAffinity(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            var affinityMask = process.ProcessorAffinity.ToInt64();
            return (true, affinityMask, process.ProcessName, "获取成功");
        }
        catch (ArgumentException)
        {
            return (false, 0, string.Empty, $"未找到进程ID：{processId}");
        }
        catch (Exception ex)
        {
            return (false, 0, string.Empty, $"获取CPU亲和性时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 为单个进程设置CPU亲和性
    /// </summary>
    private (bool Success, string Message) SetAffinityForProcess(Process process, long affinityMask)
    {
        try
        {
            process.ProcessorAffinity = new IntPtr(affinityMask);
            return (true, $"成功设置进程 {process.ProcessName}({process.Id}) 的CPU亲和性");
        }
        catch (Exception ex)
        {
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
            if (core < 0 || core > 63)  // 最多支持64个核心
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
        for (int i = 0; i < 64; i++)  // 最多检查64个核心
        {
            if ((affinityMask & (1L << i)) != 0)
            {
                cores.Add(i);
            }
        }
        return cores.ToArray();
    }
} 