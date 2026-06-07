namespace _;

public class TomlConfig
{
  public Dictionary<string, CcdConfig> Ccds { get; set; } = new();
  public string? DefaultCcd { get; set; } = null;

  /// <summary>CCD group automatically applied to detected games when game mode is on.</summary>
  public string? GameCcd { get; set; } = null;

  /// <summary>Whether automatic game detection is enabled.</summary>
  public bool GameModeEnabled { get; set; } = false;

  /// <summary>User-maintained list of process names (without .exe) always treated as games.</summary>
  public List<string> GameProcessNames { get; set; } = new();
}
public class CcdConfig
{
  public int[] Cores { get; init; } = Array.Empty<int>();
}
