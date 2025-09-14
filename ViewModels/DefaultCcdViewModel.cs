using System.Collections.ObjectModel;
using _.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace @_.ViewModels;

public partial class DefaultCcdViewModel : ObservableObject
{
    private readonly CcdService _ccdService;

    [ObservableProperty]
    private ObservableCollection<string> _availableCcds = new();

    [ObservableProperty]
    private string? _selectedCcd;

    public DefaultCcdViewModel(CcdService ccdService)
    {
        _ccdService = ccdService;
        LoadAvailableCcds();
        SelectedCcd = _ccdService.DefaultCcd ?? "Not set";
    }

    private void LoadAvailableCcds()
    {
        AvailableCcds.Clear();
        AvailableCcds.Add("Not set");
        
        foreach (var ccdName in _ccdService.Ccds.Keys)
        {
            AvailableCcds.Add(ccdName);
        }
    }
}