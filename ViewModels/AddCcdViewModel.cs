using System.Collections.ObjectModel;
using System.Windows;
using _.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace @_.ViewModels;

public partial class AddCcdViewModel : ObservableObject
{
    private readonly CcdService _ccdService;
    private readonly Window _window;

    [ObservableProperty]
    private string _ccdName = string.Empty;

    public ObservableCollection<CoreCheckBox> CoreCheckBoxes { get; }

    public AddCcdViewModel(CcdService ccdService, Window window)
    {
        _ccdService = ccdService;
        _window = window;
        
        var coreCount = ProcessAffinityService.GetProcessorCount();
        CoreCheckBoxes = new ObservableCollection<CoreCheckBox>(
            Enumerable.Range(0, coreCount)
                     .Select(i => new CoreCheckBox { CoreNumber = i, IsSelected = false }));
    }

    [RelayCommand]
    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(CcdName))
        {
            MessageBox.Show("Please enter a CCD group name", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var selectedCores = CoreCheckBoxes
            .Where(c => c.IsSelected)
            .Select(c => c.CoreNumber)
            .ToArray();

        if (selectedCores.Length == 0)
        {
            MessageBox.Show("Please select at least one CPU core", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            Console.WriteLine($"[AddCcdViewModel] Attempting to add CCD group {CcdName}, core count: {selectedCores.Length}");
            _ccdService.UpsertCcd(CcdName, selectedCores);
            DialogResult = true;
            _window.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        _window.Close();
    }

    public bool? DialogResult { get; private set; }
}

public class CoreCheckBox : ObservableObject
{
    public int CoreNumber { get; init; }
    
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
} 