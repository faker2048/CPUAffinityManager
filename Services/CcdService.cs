using System.IO;
using Tomlyn;

namespace @_.Services;

public class CcdService
{
    private readonly string _storePath;

    public Dictionary<string, CcdConfig> Ccds { get; private set; } = new();
    public string? DefaultCcd { get; private set; }
    public string? GameCcd { get; private set; }
    public bool GameModeEnabled { get; private set; }
    public List<string> GameProcessNames { get; private set; } = new();


    public CcdService()
    {
        _storePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CPUAffinityManager",
            "config.toml"
        );
        Console.WriteLine($"[CcdService] Initializing, config file path: {_storePath}");
        LoadConfig();
    }

    public void ReloadCcds()
    {
        LoadConfig();
    }

    /// <summary>
    /// Load CCD configurations and default settings
    /// </summary>
    private void LoadConfig()
    {
        try
        {
            Console.WriteLine($"[CcdService] Starting to load CCD configuration");
            if (!File.Exists(_storePath))
            {
                Console.WriteLine("[CcdService] Config file does not exist, returning empty configuration");
                Ccds = new Dictionary<string, CcdConfig>();
                DefaultCcd = null;
                GameCcd = null;
                GameModeEnabled = false;
                GameProcessNames = new List<string>();
                return;
            }

            var tomlString = File.ReadAllText(_storePath);
            var model = Toml.ToModel<TomlConfig>(tomlString);
            Console.WriteLine($"[CcdService] Successfully loaded CCD configuration, total {model?.Ccds?.Count ?? 0} CCD groups");
            Ccds = model?.Ccds ?? new Dictionary<string, CcdConfig>();
            DefaultCcd = model?.DefaultCcd;
            GameCcd = model?.GameCcd;
            GameModeEnabled = model?.GameModeEnabled ?? false;
            GameProcessNames = model?.GameProcessNames ?? new List<string>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CcdService] Failed to load CCD configuration: {ex.Message}");
            throw new Exception($"Failed to read CCD configuration: {ex.Message}", ex);
        }
    }


    /// <summary>
    /// Add or update CCD configuration
    /// </summary>
    /// <param name="ccdName">CCD name</param>
    /// <param name="cores">CPU core list</param>
    public void UpsertCcd(string ccdName, int[] cores)
    {
        Console.WriteLine($"[CcdService] Attempting to update CCD group: {ccdName}, core count: {cores.Length}");
        if (string.IsNullOrWhiteSpace(ccdName))
        {
            Console.WriteLine("[CcdService] Update failed: CCD name is empty");
            throw new ArgumentException("CCD name cannot be empty", nameof(ccdName));
        }

        if (cores == null || cores.Length == 0)
        {
            throw new ArgumentException("Must specify at least one CPU core", nameof(cores));
        }

        if (cores.Any(c => c < 0 || c > 63))
        {
            Console.WriteLine("[CcdService] Update failed: CPU core number out of range");
            throw new ArgumentException("CPU core number must be between 0-63");
        }

        Ccds[ccdName] = new CcdConfig { Cores = cores };
        SaveConfig();
        Console.WriteLine($"[CcdService] Successfully updated CCD group: {ccdName}");
    }

    /// <summary>
    /// Delete CCD configuration
    /// </summary>
    /// <param name="ccdName">CCD name to delete</param>
    /// <returns>Whether deletion was successful</returns>
    public bool DeleteCcd(string ccdName)
    {
        Console.WriteLine($"[CcdService] Attempting to delete CCD group: {ccdName}");
        var removed = Ccds.Remove(ccdName);
        if (removed)
        {
            if (DefaultCcd == ccdName) DefaultCcd = null;
            if (GameCcd == ccdName) GameCcd = null;
            SaveConfig();
            Console.WriteLine($"[CcdService] Successfully deleted CCD group: {ccdName}");
        }
        else
        {
            Console.WriteLine($"[CcdService] Delete failed: CCD group {ccdName} not found");
        }

        return removed;
    }

    public void SetDefaultCcd(string? ccdName)
    {
        Console.WriteLine($"[CcdService] Setting default CCD: {ccdName ?? "null"}");
        if (ccdName != null && !Ccds.ContainsKey(ccdName))
        {
            throw new ArgumentException($"CCD group '{ccdName}' does not exist");
        }

        DefaultCcd = ccdName;
        SaveConfig();
    }

    /// <summary>
    /// Set the CCD group automatically applied to detected games.
    /// </summary>
    public void SetGameCcd(string? ccdName)
    {
        Console.WriteLine($"[CcdService] Setting game CCD: {ccdName ?? "null"}");
        if (ccdName != null && !Ccds.ContainsKey(ccdName))
        {
            throw new ArgumentException($"CCD group '{ccdName}' does not exist");
        }

        GameCcd = ccdName;
        SaveConfig();
    }

    /// <summary>
    /// Enable or disable automatic game detection.
    /// </summary>
    public void SetGameModeEnabled(bool enabled)
    {
        Console.WriteLine($"[CcdService] Setting game mode enabled: {enabled}");
        GameModeEnabled = enabled;
        SaveConfig();
    }

    /// <summary>
    /// Replace the user-maintained list of process names always treated as games.
    /// </summary>
    public void SetGameProcessNames(IEnumerable<string> names)
    {
        GameProcessNames = names
            .Select(n => n.Trim())
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        Console.WriteLine($"[CcdService] Setting game process names, total {GameProcessNames.Count}");
        SaveConfig();
    }

    /// <summary>
    /// Save the full CCD configuration (CCD groups, default/game CCD and game list) to disk.
    /// </summary>
    private void SaveConfig()
    {
        try
        {
            Console.WriteLine("[CcdService] Starting to save CCD configuration");
            var directory = Path.GetDirectoryName(_storePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var config = new TomlConfig
            {
                Ccds = Ccds,
                DefaultCcd = DefaultCcd,
                GameCcd = GameCcd,
                GameModeEnabled = GameModeEnabled,
                GameProcessNames = GameProcessNames
            };
            var tomlString = Toml.FromModel(config);
            File.WriteAllText(_storePath, tomlString);
            Console.WriteLine($"[CcdService] Successfully saved CCD configuration, total {Ccds.Count} CCD groups, default: {DefaultCcd ?? "none"}, game: {GameCcd ?? "none"}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CcdService] Failed to save CCD configuration: {ex.Message}");
            throw new Exception($"Failed to save CCD configuration: {ex.Message}", ex);
        }
    }
}