using System;

namespace Assist.Core.Models;

public sealed class MatchSummary
{
    public required string MatchId { get; init; }

    public required string Map { get; init; }

    public required string Mode { get; init; }

    public required string Agent { get; init; }

    public required string Result { get; init; }

    public int RoundsWon { get; init; }

    public int RoundsLost { get; init; }

    public double CombatScore { get; init; }

    public double Kda { get; init; }

    public DateTimeOffset CompletedAt { get; init; }
}
