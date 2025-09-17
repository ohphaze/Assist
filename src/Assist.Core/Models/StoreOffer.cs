using System;

namespace Assist.Core.Models;

public sealed class StoreOffer
{
    public required string OfferId { get; init; }

    public required string ItemName { get; init; }

    public required string ItemType { get; init; }

    public required string Currency { get; init; }

    public int Cost { get; init; }

    public string? ImageUrl { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }
}
