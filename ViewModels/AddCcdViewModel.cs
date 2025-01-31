using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace _;

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
            MessageBox.Show("请输入CCD组名称", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var selectedCores = CoreCheckBoxes
            .Where(c => c.IsSelected)
            .Select(c => c.CoreNumber)
            .ToArray();

        if (selectedCores.Length == 0)
        {
            MessageBox.Show("请至少选择一个CPU核心", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            Console.WriteLine($"[AddCcdViewModel] 尝试添加CCD组 {CcdName}，核心数：{selectedCores.Length}");
            _ccdService.UpsertCcd(CcdName, selectedCores);
            DialogResult = true;
            _window.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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