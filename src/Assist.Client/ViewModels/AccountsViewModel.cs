using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Assist.Core.Abstractions;
using Assist.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Assist.Client.ViewModels;

public sealed partial class AccountsViewModel : ViewModelBase
{
    private readonly IAccountService _accountService;

    public AccountsViewModel(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public ObservableCollection<ValorantAccount> Accounts { get; } = new();

    public string[] Regions { get; } = { "NA", "EU", "AP", "KR", "BR", "LATAM" };

    [ObservableProperty]
    private ValorantAccount? _selectedAccount;

    [ObservableProperty]
    private string _region = "NA";

    [ObservableProperty]
    private string? _displayName;

    [ObservableProperty]
    private string? _tagLine;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private bool _requiresSecondFactor;

    [ObservableProperty]
    private string? _error;

    [ObservableProperty]
    private bool _isBusy;

    public override async Task OnNavigatedToAsync()
    {
        await base.OnNavigatedToAsync();
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        try
        {
            IsBusy = true;
            Error = null;

            Accounts.Clear();
            var accounts = await _accountService.GetAccountsAsync().ConfigureAwait(false);
            foreach (var account in accounts)
            {
                Accounts.Add(account);
            }

            var active = await _accountService.GetActiveAccountAsync().ConfigureAwait(false);
            SelectedAccount = active ?? Accounts.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddAccountAsync()
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            Error = "Display name is required.";
            return;
        }

        try
        {
            IsBusy = true;
            Error = null;

            var account = new ValorantAccount
            {
                DisplayName = DisplayName.Trim(),
                TagLine = string.IsNullOrWhiteSpace(TagLine) ? null : TagLine.TrimStart('#'),
                Region = Region,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                RequiresSecondFactor = RequiresSecondFactor
            };

            var saved = await _accountService.AddOrUpdateAsync(account).ConfigureAwait(false);
            await RefreshAsync().ConfigureAwait(false);
            SelectedAccount = saved;
            await _accountService.SetActiveAccountAsync(saved.Id).ConfigureAwait(false);

            DisplayName = string.Empty;
            TagLine = string.Empty;
            Notes = string.Empty;
            RequiresSecondFactor = false;
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RemoveSelectedAsync()
    {
        if (SelectedAccount is null)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await _accountService.RemoveAsync(SelectedAccount.Id).ConfigureAwait(false);
            await RefreshAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SetActiveAsync(ValorantAccount account)
    {
        await _accountService.SetActiveAccountAsync(account.Id).ConfigureAwait(false);
        await RefreshAsync().ConfigureAwait(false);
    }
}
