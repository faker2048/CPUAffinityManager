namespace _;

public class TomlConfig
{
  public Dictionary<string, CcdConfig> Ccds { get; set; } = new();
  public string? DefaultCcd { get; set; } = null;
}
public class CcdConfig
{
  public int[] Cores { get; init; } = Array.Empty<int>();
}
