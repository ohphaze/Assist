using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Assist.Core.Abstractions;
using Assist.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Assist.Client.ViewModels;

public sealed partial class DashboardViewModel : ViewModelBase
{
    private readonly IAccountService _accountService;
    private readonly IValorantDataService _dataService;

    public DashboardViewModel(IAccountService accountService, IValorantDataService dataService)
    {
        _accountService = accountService;
        _dataService = dataService;
    }

    public ObservableCollection<DailyMission> DailyMissions { get; } = new();

    public ObservableCollection<MatchSummary> RecentMatches { get; } = new();

    public ObservableCollection<NewsArticle> NewsArticles { get; } = new();

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

            var active = await _accountService.GetActiveAccountAsync().ConfigureAwait(false);
            DailyMissions.Clear();
            RecentMatches.Clear();
            NewsArticles.Clear();

            var missions = await _dataService.GetDailyMissionsAsync(active?.Id).ConfigureAwait(false);
            foreach (var mission in missions)
            {
                DailyMissions.Add(mission);
            }

            var matches = await _dataService.GetRecentMatchesAsync(active?.Id).ConfigureAwait(false);
            foreach (var match in matches)
            {
                RecentMatches.Add(match);
            }

            var news = await _dataService.GetNewsAsync().ConfigureAwait(false);
            foreach (var article in news)
            {
                NewsArticles.Add(article);
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
