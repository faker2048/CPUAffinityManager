using System.Windows;

namespace _;

public partial class AddProcessWindow : Window
{
    public AddProcessWindow(AddProcessViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.SetWindow(this);
        Console.WriteLine("[AddProcessWindow] 窗口初始化完成");
    }
} 