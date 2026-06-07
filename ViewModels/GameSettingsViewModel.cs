using System.Collections.ObjectModel;
using _.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace @_.ViewModels;

public partial class GameSettingsViewModel : ObservableObject
{
    public const string NotSet = "Not set";

    private readonly CcdService _ccdService;

    [ObservableProperty]
    private ObservableCollection<string> _availableCcds = new();

    [ObservableProperty]
    private string? _selectedCcd;

    /// <summary>Game process names, one per line (without .exe).</summary>
    [ObservableProperty]
    private string _gameNamesText = string.Empty;

    public GameSettingsViewModel(CcdService ccdService)
    {
        _ccdService = ccdService;

        AvailableCcds.Add(NotSet);
        foreach (var ccdName in _ccdService.Ccds.Keys)
        {
            AvailableCcds.Add(ccdName);
        }

        SelectedCcd = _ccdService.GameCcd ?? NotSet;
        GameNamesText = string.Join(Environment.NewLine, _ccdService.GameProcessNames);
    }

    /// <summary>The selected game CCD, or null when "Not set".</summary>
    public string? ResolvedGameCcd => SelectedCcd == NotSet ? null : SelectedCcd;

    public IEnumerable<string> GameNames =>
        GameNamesText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(StripExeSuffix);

    private static string StripExeSuffix(string name)
    {
        var trimmed = name.Trim();
        return trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? trimmed[..^4]
            : trimmed;
    }
}
