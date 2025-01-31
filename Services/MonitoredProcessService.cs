using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Newtonsoft.Json;

namespace _;

public class MonitoredProcessService
{
    private Dictionary<string, MonitoredProcess> _monitoredProcessesCache; // <ProcessName, MonitoredProcess>
    public Dictionary<string, MonitoredProcess> MonitoredProcesses => _monitoredProcessesCache;
    private string _storePath;

    public MonitoredProcessService()
    {
        _storePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CPUAffinityManager",
            "monitored_processes.json"
        );
        _monitoredProcessesCache = LoadMonitoredProcesses(_storePath);
    }

    private static Dictionary<string, MonitoredProcess> LoadMonitoredProcesses(string storePath)
    {
        try
        {
            Console.WriteLine($"[MonitoredProcessService] 开始加载监控进程配置，配置文件路径：{storePath}");
            if (!File.Exists(storePath))
            {
                Console.WriteLine("[MonitoredProcessService] 配置文件不存在，返回空配置");
                Directory.CreateDirectory(Path.GetDirectoryName(storePath) ?? string.Empty);
                return new Dictionary<string, MonitoredProcess>();
            }

            var json = File.ReadAllText(storePath);
            if (string.IsNullOrEmpty(json))
            {
                Console.WriteLine("[MonitoredProcessService] 配置文件为空，返回空配置");
                return new Dictionary<string, MonitoredProcess>();
            }

            var monitoredProcesses = JsonConvert.DeserializeObject<List<MonitoredProcess>>(json);
            Console.WriteLine($"[MonitoredProcessService] 成功加载监控进程配置，共 {monitoredProcesses?.Count ?? 0} 个进程");
            return monitoredProcesses?.ToDictionary(p => p.ProcessName, p => p)
                   ?? new Dictionary<string, MonitoredProcess>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MonitoredProcessService] 加载监控进程配置失败：{ex.Message}");
            MessageBox.Show($"加载配置文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return new Dictionary<string, MonitoredProcess>();
        }
    }

    public void SaveMonitoredProcesses(Dictionary<string, MonitoredProcess> monitoredProcesses)
    {
        Console.WriteLine("[MonitoredProcessService] 开始保存监控进程配置");
        if (string.IsNullOrEmpty(_storePath))
        {
            Console.WriteLine("[MonitoredProcessService] 保存失败：配置文件路径为空");
            throw new InvalidOperationException("配置文件路径未设置");
        }

        var directory = Path.GetDirectoryName(_storePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }


        var config = monitoredProcesses.Values.ToList();
        var json = JsonConvert.SerializeObject(config, Formatting.Indented);
        File.WriteAllText(_storePath, json);
        Console.WriteLine($"[MonitoredProcessService] 成功保存监控进程配置，共 {config.Count} 个进程");
    }

    public void AddMonitoredProcess(MonitoredProcess monitoredProcess)
    {
        Console.WriteLine($"[MonitoredProcessService] 尝试添加监控进程：{monitoredProcess?.ProcessName}");
        if (monitoredProcess == null)
        {
            Console.WriteLine("[MonitoredProcessService] 添加失败：进程对象为空");
            throw new ArgumentNullException(nameof(monitoredProcess));
        }

        if (string.IsNullOrEmpty(monitoredProcess.ProcessName))
        {
            Console.WriteLine("[MonitoredProcessService] 添加失败：进程名称为空");
            throw new ArgumentException("进程名称不能为空", nameof(monitoredProcess));
        }

        var tmp = new Dictionary<string, MonitoredProcess>(_monitoredProcessesCache);
        tmp[monitoredProcess.ProcessName] = monitoredProcess;
        SaveMonitoredProcesses(tmp);
        _monitoredProcessesCache = tmp;
        Console.WriteLine($"[MonitoredProcessService] 成功添加监控进程：{monitoredProcess.ProcessName}");
    }

    public void RemoveMonitoredProcess(string processName)
    {
        Console.WriteLine($"[MonitoredProcessService] 尝试删除监控进程：{processName}");
        if (string.IsNullOrEmpty(processName))
        {
            Console.WriteLine("[MonitoredProcessService] 删除失败：进程名称为空");
            throw new ArgumentException("进程名称不能为空", nameof(processName));
        }

        var tmp = new Dictionary<string, MonitoredProcess>(_monitoredProcessesCache);
        if (tmp.Remove(processName))
        {
            SaveMonitoredProcesses(tmp);
            _monitoredProcessesCache = tmp;
            Console.WriteLine($"[MonitoredProcessService] 成功删除监控进程：{processName}");
        }
        else
        {
            Console.WriteLine($"[MonitoredProcessService] 删除失败：未找到进程 {processName}");
        }
    }
}