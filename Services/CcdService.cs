using System.IO;
using Tomlyn;

namespace @_.Services;

public class CcdService
{
    private readonly string _storePath;

    public Dictionary<string, CcdConfig> Ccds { get; private set; }


    public CcdService()
    {
        Console.WriteLine($"[CcdService] 初始化，配置文件路径：{_storePath}");
        _storePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CPUAffinityManager",
            "config.toml"
        );
        Ccds = LoadCcds();
    }

    public void ReloadCcds()
    {
        Ccds = LoadCcds();
    }

    /// <summary>
    /// 获取所有 CCD 配置
    /// </summary>
    private Dictionary<string, CcdConfig> LoadCcds()
    {
        try
        {
            Console.WriteLine($"[CcdService] 开始加载CCD配置");
            if (!File.Exists(_storePath))
            {
                Console.WriteLine("[CcdService] 配置文件不存在，返回空配置");
                return new Dictionary<string, CcdConfig>();
            }

            var tomlString = File.ReadAllText(_storePath);
            var model = Toml.ToModel<TomlConfig>(tomlString);
            Console.WriteLine($"[CcdService] 成功加载CCD配置，共 {model?.Ccds?.Count ?? 0} 个CCD组");
            return model?.Ccds ?? new Dictionary<string, CcdConfig>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CcdService] 加载CCD配置失败：{ex.Message}");
            throw new Exception($"读取 CCD 配置失败: {ex.Message}", ex);
        }
    }


    /// <summary>
    /// 添加或更新 CCD 配置
    /// </summary>
    /// <param name="ccdName">CCD 名称</param>
    /// <param name="cores">CPU 核心列表</param>
    public void UpsertCcd(string ccdName, int[] cores)
    {
        Console.WriteLine($"[CcdService] 尝试更新CCD组：{ccdName}，核心数：{cores.Length}");
        if (string.IsNullOrWhiteSpace(ccdName))
        {
            Console.WriteLine("[CcdService] 更新失败：CCD名称为空");
            throw new ArgumentException("CCD 名称不能为空", nameof(ccdName));
        }

        if (cores == null || cores.Length == 0)
        {
            throw new ArgumentException("必须指定至少一个 CPU 核心", nameof(cores));
        }

        if (cores.Any(c => c < 0 || c > 63))
        {
            Console.WriteLine("[CcdService] 更新失败：CPU核心编号超出范围");
            throw new ArgumentException("CPU 核心编号必须在 0-63 之间");
        }

        var ccds = Ccds;
        ccds[ccdName] = new CcdConfig { Cores = cores };
        SaveCcds(ccds);
        Console.WriteLine($"[CcdService] 成功更新CCD组：{ccdName}");
    }

    /// <summary>
    /// 删除 CCD 配置
    /// </summary>
    /// <param name="ccdName">要删除的 CCD 名称</param>
    /// <returns>是否删除成功</returns>
    public bool DeleteCcd(string ccdName)
    {
        Console.WriteLine($"[CcdService] 尝试删除CCD组：{ccdName}");
        var ccds = Ccds;
        var removed = ccds.Remove(ccdName);
        if (removed)
        {
            SaveCcds(ccds);
            Console.WriteLine($"[CcdService] 成功删除CCD组：{ccdName}");
        }
        else
        {
            Console.WriteLine($"[CcdService] 删除失败：未找到CCD组 {ccdName}");
        }

        return removed;
    }

    /// <summary>
    /// 保存 CCD 配置
    /// </summary>
    private void SaveCcds(Dictionary<string, CcdConfig> ccds)
    {
        try
        {
            Console.WriteLine("[CcdService] 开始保存CCD配置");
            var directory = Path.GetDirectoryName(_storePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var config = new TomlConfig { Ccds = ccds };
            var tomlString = Toml.FromModel(config);
            File.WriteAllText(_storePath, tomlString);
            Ccds = ccds;
            Console.WriteLine($"[CcdService] 成功保存CCD配置，共 {ccds.Count} 个CCD组");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CcdService] 保存CCD配置失败：{ex.Message}");
            throw new Exception($"保存 CCD 配置失败: {ex.Message}", ex);
        }
    }
}