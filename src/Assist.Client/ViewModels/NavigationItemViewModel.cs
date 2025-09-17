using CommunityToolkit.Mvvm.ComponentModel;

namespace Assist.Client.ViewModels;

public sealed partial class NavigationItemViewModel : ObservableObject
{
    public NavigationItemViewModel(string title, string icon, ViewModelBase viewModel)
    {
        Title = title;
        Icon = icon;
        ViewModel = viewModel;
    }

    public string Title { get; }

    public string Icon { get; }

    public ViewModelBase ViewModel { get; }

    [ObservableProperty]
    private bool _isSelected;
}
