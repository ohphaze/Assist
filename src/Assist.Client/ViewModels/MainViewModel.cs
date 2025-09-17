using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Assist.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Assist.Client.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private readonly IAccountService _accountService;
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly AccountsViewModel _accountsViewModel;
    private readonly StoreViewModel _storeViewModel;
    private readonly LiveViewModel _liveViewModel;
    private readonly ModulesViewModel _modulesViewModel;
    private readonly SettingsViewModel _settingsViewModel;

    public MainViewModel(
        IAccountService accountService,
        DashboardViewModel dashboardViewModel,
        AccountsViewModel accountsViewModel,
        StoreViewModel storeViewModel,
        LiveViewModel liveViewModel,
        ModulesViewModel modulesViewModel,
        SettingsViewModel settingsViewModel)
    {
        _accountService = accountService;
        _dashboardViewModel = dashboardViewModel;
        _accountsViewModel = accountsViewModel;
        _storeViewModel = storeViewModel;
        _liveViewModel = liveViewModel;
        _modulesViewModel = modulesViewModel;
        _settingsViewModel = settingsViewModel;

        NavigationItems = new ObservableCollection<NavigationItemViewModel>
        {
            new("Dashboard", "🏠", _dashboardViewModel),
            new("Accounts", "👤", _accountsViewModel),
            new("Store", "🛒", _storeViewModel),
            new("Live", "🎮", _liveViewModel),
            new("Modules", "🧩", _modulesViewModel),
            new("Settings", "⚙️", _settingsViewModel)
        };

        _accountService.AccountsChanged += OnAccountsChanged;
    }

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

    [ObservableProperty]
    private NavigationItemViewModel? _selectedNavigationItem;

    [ObservableProperty]
    private ViewModelBase? _currentViewModel;

    [ObservableProperty]
    private string? _activeAccount;

    [ObservableProperty]
    private bool _hasActiveAccount;

    public override async Task OnNavigatedToAsync()
    {
        await base.OnNavigatedToAsync();
        await LoadActiveAccountAsync();
        SelectedNavigationItem ??= NavigationItems.FirstOrDefault();
    }

    partial void OnSelectedNavigationItemChanged(NavigationItemViewModel? value)
    {
        if (value is null)
        {
            return;
        }

        foreach (var item in NavigationItems)
        {
            item.IsSelected = item == value;
        }

        var previous = CurrentViewModel;
        CurrentViewModel = value.ViewModel;
        _ = previous?.OnNavigatedFromAsync();
        _ = value.ViewModel.OnNavigatedToAsync();
    }

    private async void OnAccountsChanged(object? sender, EventArgs e)
    {
        await LoadActiveAccountAsync();
        if (CurrentViewModel is AccountsViewModel accounts)
        {
            await accounts.RefreshAsync();
        }
    }

    private async Task LoadActiveAccountAsync()
    {
        var active = await _accountService.GetActiveAccountAsync().ConfigureAwait(false);
        if (active is null)
        {
            ActiveAccount = "No active account";
            HasActiveAccount = false;
        }
        else
        {
            var tag = string.IsNullOrWhiteSpace(active.TagLine) ? string.Empty : $"#{active.TagLine}";
            ActiveAccount = $"{active.DisplayName}{tag}";
            HasActiveAccount = true;
        }
    }
}
