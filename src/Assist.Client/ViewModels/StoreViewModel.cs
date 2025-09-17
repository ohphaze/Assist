using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Assist.Core.Abstractions;
using Assist.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Assist.Client.ViewModels;

public sealed partial class StoreViewModel : ViewModelBase
{
    private readonly IAccountService _accountService;
    private readonly IValorantDataService _dataService;

    public StoreViewModel(IAccountService accountService, IValorantDataService dataService)
    {
        _accountService = accountService;
        _dataService = dataService;
    }

    public ObservableCollection<StoreOffer> Offers { get; } = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _error;

    public override async Task OnNavigatedToAsync()
    {
        await base.OnNavigatedToAsync();
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            Error = null;
            Offers.Clear();
            var active = await _accountService.GetActiveAccountAsync().ConfigureAwait(false);
            var offers = await _dataService.GetStoreOffersAsync(active?.Id).ConfigureAwait(false);
            foreach (var offer in offers)
            {
                Offers.Add(offer);
            }
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
