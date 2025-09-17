using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Assist.Core.Models;

namespace Assist.Core.Abstractions;

public interface IValorantDataService
{
    Task<IReadOnlyList<DailyMission>> GetDailyMissionsAsync(Guid? accountId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MatchSummary>> GetRecentMatchesAsync(Guid? accountId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoreOffer>> GetStoreOffersAsync(Guid? accountId, CancellationToken cancellationToken = default);

    Task<LiveGameInfo?> GetLiveGameAsync(Guid? accountId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NewsArticle>> GetNewsAsync(CancellationToken cancellationToken = default);
}
