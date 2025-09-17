using System;

namespace Assist.Core.Models;

/// <summary>
///     Represents a Valorant account that can be launched through Assist.
/// </summary>
public sealed class ValorantAccount
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    ///     Friendly name that is displayed inside the UI.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    ///     Valorant tagline (e.g. #NA1). Optional because some players prefer hidden accounts.
    /// </summary>
    public string? TagLine { get; init; }

    /// <summary>
    ///     Riot region for the account. Used for store calls and localization.
    /// </summary>
    public required string Region { get; init; }

    /// <summary>
    ///     Timestamp when the account was added to Assist.
    /// </summary>
    public DateTimeOffset AddedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    ///     Timestamp of the last time the account was launched from Assist.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; init; }

    /// <summary>
    ///     Optional notes that the user wants to keep alongside the account (for example smurf indicators).
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    ///     When true the UI should prompt for a two factor code when launching.
    /// </summary>
    public bool RequiresSecondFactor { get; init; }

    public ValorantAccount WithId(Guid id)
        => new()
        {
            Id = id,
            DisplayName = DisplayName,
            TagLine = TagLine,
            Region = Region,
            AddedAt = AddedAt,
            LastUsedAt = LastUsedAt,
            Notes = Notes,
            RequiresSecondFactor = RequiresSecondFactor
        };

    /// <summary>
    ///     Returns a new <see cref="ValorantAccount"/> with the provided <paramref name="lastUsed"/> timestamp.
    /// </summary>
    public ValorantAccount WithLastUsed(DateTimeOffset? lastUsed)
        => new()
        {
            Id = Id,
            DisplayName = DisplayName,
            TagLine = TagLine,
            Region = Region,
            AddedAt = AddedAt,
            LastUsedAt = lastUsed,
            Notes = Notes,
            RequiresSecondFactor = RequiresSecondFactor
        };
}
