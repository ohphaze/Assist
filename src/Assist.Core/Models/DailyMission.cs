using System;

namespace Assist.Core.Models;

/// <summary>
///     Represents one of the daily missions exposed by Riot in Valorant.
/// </summary>
public sealed class DailyMission
{
    public required string Title { get; init; }

    public string? Description { get; init; }

    public int Progress { get; init; }

    public int Target { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }

    public double Completion => Target == 0 ? 0 : Math.Clamp((double)Progress / Target, 0, 1);
}
