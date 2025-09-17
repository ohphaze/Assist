using System;
using System.Collections.Generic;
using System.Linq;

namespace Assist.Core.Models;

public sealed class LiveGameInfo
{
    public required string MatchId { get; init; }

    public required string Map { get; init; }

    public required string Mode { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public required IReadOnlyList<PlayerPerformance> AllyTeam { get; init; }

    public required IReadOnlyList<PlayerPerformance> EnemyTeam { get; init; }

    public int? RoundNumber { get; init; }

    public TimeSpan MatchDuration => DateTimeOffset.UtcNow - StartedAt;

    public IReadOnlyList<PlayerPerformance> AllPlayers => AllyTeam.Concat(EnemyTeam).ToList();
}

public sealed class PlayerPerformance
{
    public required string DisplayName { get; init; }

    public required string Agent { get; init; }

    public int Kills { get; init; }

    public int Deaths { get; init; }

    public int Assists { get; init; }

    public double CombatScore { get; init; }
}
