using System;
using Assist.ViewModels.Dashboard;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Assist.Controls.Dashboard;

public partial class ProgressionPreviewControl : UserControl
{
    private readonly ProgressionPreviewViewModel _viewModel;

    public ProgressionPreviewControl()
    {
        DataContext = _viewModel = new ProgressionPreviewViewModel();
        InitializeComponent();
    }

    private async void ProgressionPreview_Init(object? sender, EventArgs e)
    {
        if (!Design.IsDesignMode)
        {
            try { await _viewModel.Setup(); }
            catch (System.Exception ex)
            {
                Serilog.Log.Error("ProgressionPreview_Init failed: {Message}", ex.Message);
                Serilog.Log.Error(ex.StackTrace);
            }
        }
    }

    private async void ProgressionPreview_Loaded(object? sender, RoutedEventArgs e)
    {
        if (!Design.IsDesignMode)
        {
            try { await _viewModel.LoadedCheck(); }
            catch (System.Exception ex)
            {
                Serilog.Log.Error("ProgressionPreview_Loaded failed: {Message}", ex.Message);
                Serilog.Log.Error(ex.StackTrace);
            }
        }
    }
}
