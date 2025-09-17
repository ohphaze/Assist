using System;

namespace Assist.Core.Models;

public sealed class NewsArticle
{
    public required string Title { get; init; }

    public string? Summary { get; init; }

    public Uri? Url { get; init; }

    public DateTimeOffset PublishedAt { get; init; }
}
