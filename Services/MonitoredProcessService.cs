using System.IO;
using System.Windows;
using Newtonsoft.Json;

namespace @_.Services;

public class MonitoredProcessService
{
    public Dictionary<string, MonitoredProcess> MonitoredProcesses { get; private set; }

    private readonly string _storePath;
    private readonly CcdService _ccdService;
    public bool IsAutoApplyRules { get; set; }

    public event Action<ProcessInfo>? MonitoredProcessStarted;
    public event Action<ProcessInfo>? MonitoredProcessEnded;
    public event Action<ProcessInfo>? MonitoredProcessAffinityChanged;



    public MonitoredProcessService(ProcessMonitorService processMonitorService, CcdService ccdService)

    {
        _ccdService = ccdService;
        processMonitorService.ProcessStarted += OnProcessStarted;
        processMonitorService.ProcessEnded += OnProcessEnded;
        processMonitorService.ProcessAffinityChanged += OnProcessAffinityChanged;

        _storePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CPUAffinityManager",
            "monitored_processes.json"
        );
        MonitoredProcesses = LoadMonitoredProcesses(_storePath);

    }

    private static Dictionary<string, MonitoredProcess> LoadMonitoredProcesses(string storePath)
    {
        try
        {
            Console.WriteLine($"[MonitoredProcessService] Loading monitored process configuration, config file path: {storePath}");
            if (!File.Exists(storePath))
            {
                Console.WriteLine("[MonitoredProcessService] Config file does not exist, returning empty configuration");
                Directory.CreateDirectory(Path.GetDirectoryName(storePath) ?? string.Empty);
                return new Dictionary<string, MonitoredProcess>();
            }

            var json = File.ReadAllText(storePath);
            if (string.IsNullOrEmpty(json))
            {
                Console.WriteLine("[MonitoredProcessService] Config file is empty, returning empty configuration");
                return new Dictionary<string, MonitoredProcess>();
            }

            var monitoredProcesses = JsonConvert.DeserializeObject<List<MonitoredProcess>>(json);
            Console.WriteLine($"[MonitoredProcessService] Successfully loaded monitored process configuration, total {monitoredProcesses?.Count ?? 0} processes");
            return monitoredProcesses?.ToDictionary(p => p.ProcessName, p => p)
                   ?? new Dictionary<string, MonitoredProcess>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MonitoredProcessService] Failed to load monitored process configuration: {ex.Message}");
            MessageBox.Show($"Failed to load configuration file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return new Dictionary<string, MonitoredProcess>();
        }
    }

    public void SaveMonitoredProcesses(Dictionary<string, MonitoredProcess> monitoredProcesses)
    {
        Console.WriteLine("[MonitoredProcessService] Starting to save monitored process configuration");
        if (string.IsNullOrEmpty(_storePath))
        {
            Console.WriteLine("[MonitoredProcessService] Save failed: Config file path is empty");
            throw new InvalidOperationException("Config file path is not set");
        }

        var directory = Path.GetDirectoryName(_storePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var config = monitoredProcesses.Values.ToList();
        var json = JsonConvert.SerializeObject(config, Formatting.Indented);
        File.WriteAllText(_storePath, json);
        Console.WriteLine($"[MonitoredProcessService] Successfully saved monitored process configuration, total {config.Count} processes");
    }

    public void AddMonitoredProcess(MonitoredProcess monitoredProcess)
    {
        Console.WriteLine($"[MonitoredProcessService] Attempting to add monitored process: {monitoredProcess?.ProcessName}");
        if (monitoredProcess == null)
        {
            Console.WriteLine("[MonitoredProcessService] Add failed: Process object is null");
            throw new ArgumentNullException(nameof(monitoredProcess));
        }

        if (string.IsNullOrEmpty(monitoredProcess.ProcessName))
        {
            Console.WriteLine("[MonitoredProcessService] Add failed: Process name is empty");
            throw new ArgumentException("Process name cannot be empty", nameof(monitoredProcess));
        }

        var tmp = new Dictionary<string, MonitoredProcess>(MonitoredProcesses);
        tmp[monitoredProcess.ProcessName] = monitoredProcess;
        SaveMonitoredProcesses(tmp);
        MonitoredProcesses = tmp;
        Console.WriteLine($"[MonitoredProcessService] Successfully added monitored process: {monitoredProcess.ProcessName}");
    }

    public void RemoveMonitoredProcess(string processName)
    {
        Console.WriteLine($"[MonitoredProcessService] Attempting to delete monitored process: {processName}");
        if (string.IsNullOrEmpty(processName))
        {
            Console.WriteLine("[MonitoredProcessService] Delete failed: Process name is empty");
            throw new ArgumentException("Process name cannot be empty", nameof(processName));
        }

        var tmp = new Dictionary<string, MonitoredProcess>(MonitoredProcesses);
        if (tmp.Remove(processName))
        {
            SaveMonitoredProcesses(tmp);
            MonitoredProcesses = tmp;
            Console.WriteLine($"[MonitoredProcessService] Successfully deleted monitored process: {processName}");
        }
        else
        {
            Console.WriteLine($"[MonitoredProcessService] Delete failed: Process {processName} not found");
        }
    }

    private bool IsProcessMonitored(string processName)
    {
        return MonitoredProcesses.ContainsKey(processName);
    }

    private void OnProcessStarted(ProcessInfo processInfo)
    {
        if (IsProcessMonitored(processInfo.ProcessName))
        {
            if (IsAutoApplyRules)
            {
                var monitoredProcess = MonitoredProcesses[processInfo.ProcessName];

                var ccdConfig = _ccdService.Ccds.GetValueOrDefault(monitoredProcess.CcdName);
                if (ccdConfig == null)
                {
                    Console.WriteLine($"[MonitoredProcessService] CCD configuration not found: {monitoredProcess.CcdName}");
                    return;
                }

                var affinityMask = ProcessAffinityService.CreateAffinityMask(ccdConfig.Cores.ToArray());
                var result = ProcessAffinityService.SetAffinityById(processInfo.ProcessId, affinityMask);
                if (!result.Success)
                {
                    Console.WriteLine($"[MonitoredProcessService] Failed to set CPU affinity: {result.Message}");
                }
            }

            MonitoredProcessStarted?.Invoke(processInfo);
        }
        else if (IsAutoApplyRules && !string.IsNullOrEmpty(_ccdService.DefaultCcd))
        {
            var defaultCcdConfig = _ccdService.Ccds.GetValueOrDefault(_ccdService.DefaultCcd);
            if (defaultCcdConfig != null)
            {
                var affinityMask = ProcessAffinityService.CreateAffinityMask(defaultCcdConfig.Cores.ToArray());
                var result = ProcessAffinityService.SetAffinityById(processInfo.ProcessId, affinityMask);
                if (!result.Success)
                {
                    Console.WriteLine($"[MonitoredProcessService] Failed to set default CCD: {result.Message}");
                }
                else
                {
                    Console.WriteLine($"[MonitoredProcessService] Applied default CCD {_ccdService.DefaultCcd} to process {processInfo.ProcessName}");
                }
            }
        }
    }


    private void OnProcessEnded(ProcessInfo processInfo)
    {
        if (!IsProcessMonitored(processInfo.ProcessName))
        {
            return;
        }

        MonitoredProcessEnded?.Invoke(processInfo);
    }

    private void OnProcessAffinityChanged(ProcessInfo processInfo)

    {
        if (!IsProcessMonitored(processInfo.ProcessName))
        {
            return;
        }

        MonitoredProcessAffinityChanged?.Invoke(processInfo);
    }
}