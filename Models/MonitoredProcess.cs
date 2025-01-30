using CommunityToolkit.Mvvm.ComponentModel;

namespace _;

public partial class MonitoredProcess : ObservableObject
{
    [ObservableProperty]
    private int _processId;

    [ObservableProperty]
    private string _processName = string.Empty;

    [ObservableProperty]
    private string _enabledCores = string.Empty;
} 