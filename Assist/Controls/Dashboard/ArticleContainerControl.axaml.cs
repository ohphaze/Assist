using System;
using System.Threading.Tasks;
using Assist.ViewModels.Dashboard;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Assist.Controls.Dashboard;

public partial class ArticleContainerControl : UserControl
{
    private readonly ArticleContainerViewModel _viewModel;

    public ArticleContainerControl()
    {
        DataContext = _viewModel = new ArticleContainerViewModel();
        InitializeComponent();
    }

    private async void ArticleContainer_Init(object? sender, EventArgs e)
    {
        if (Design.IsDesignMode)
            return; 
        try
        {
            _viewModel.Setup();
        }
        catch (System.Exception ex)
        {
            Serilog.Log.Error("ArticleContainer_Init failed: {Message}", ex.Message);
            Serilog.Log.Error(ex.StackTrace);
        }
    }

    private void ArticleContainer_Loaded(object? sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return; 
        try
        {
            _viewModel.Refresh();
        }
        catch (System.Exception ex)
        {
            Serilog.Log.Error("ArticleContainer_Loaded failed: {Message}", ex.Message);
            Serilog.Log.Error(ex.StackTrace);
        }
    }
}
