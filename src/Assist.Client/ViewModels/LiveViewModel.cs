using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Assist.Core.Abstractions;
using Assist.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Assist.Client.ViewModels;

public sealed partial class LiveViewModel : ViewModelBase
{
    private readonly IAccountService _accountService;
    private readonly IValorantDataService _dataService;

    public LiveViewModel(IAccountService accountService, IValorantDataService dataService)
    {
        _accountService = accountService;
        _dataService = dataService;
    }

    public ObservableCollection<PlayerPerformance> AllyTeam { get; } = new();

    public ObservableCollection<PlayerPerformance> EnemyTeam { get; } = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _error;

    [ObservableProperty]
    private LiveGameInfo? _currentMatch;

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
            AllyTeam.Clear();
            EnemyTeam.Clear();
            CurrentMatch = null;

            var active = await _accountService.GetActiveAccountAsync().ConfigureAwait(false);
            var liveGame = await _dataService.GetLiveGameAsync(active?.Id).ConfigureAwait(false);
            CurrentMatch = liveGame;

            if (liveGame is not null)
            {
                foreach (var player in liveGame.AllyTeam)
                {
                    AllyTeam.Add(player);
                }

                foreach (var player in liveGame.EnemyTeam)
                {
                    EnemyTeam.Add(player);
                }
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
