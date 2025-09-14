using System.Windows;
using _.ViewModels;

namespace _;

/// <summary>
/// Dialog window for setting the default CCD configuration
/// </summary>
public partial class DefaultCcdWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the DefaultCcdWindow class
    /// </summary>
    /// <param name="viewModel">The view model for the default CCD configuration</param>
    public DefaultCcdWindow(DefaultCcdViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    /// <summary>
    /// Handles the OK button click event
    /// </summary>
    /// <param name="sender">The event sender</param>
    /// <param name="e">The event arguments</param>
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    /// <summary>
    /// Handles the Cancel button click event
    /// </summary>
    /// <param name="sender">The event sender</param>
    /// <param name="e">The event arguments</param>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}