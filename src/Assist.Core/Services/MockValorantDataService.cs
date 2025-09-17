using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Assist.Core.Abstractions;
using Assist.Core.Models;

namespace Assist.Core.Services;

public sealed class MockValorantDataService : IValorantDataService
{
    private static readonly string[] Maps =
    {
        "Ascent", "Bind", "Haven", "Split", "Fracture", "Pearl", "Lotus"
    };

    private static readonly string[] Agents =
    {
        "Jett", "Raze", "Sage", "Astra", "Brimstone", "Gekko", "Skye", "Iso", "Chamber"
    };

    private static readonly string[] Modes =
    {
        "Unrated", "Competitive", "Swiftplay", "Team Deathmatch"
    };

    private static readonly string[] MissionVerbs =
    {
        "Eliminate", "Plant Spikes", "Defuse Spikes", "Win Rounds", "Use Abilities"
    };

    private static readonly string[] NewsTitles =
    {
        "Patch Notes", "New Agent Spotlight", "Esports Update", "Community Highlights"
    };

    private static readonly TimeSpan SimulatedDelay = TimeSpan.FromMilliseconds(250);

    public async Task<IReadOnlyList<DailyMission>> GetDailyMissionsAsync(Guid? accountId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(SimulatedDelay, cancellationToken).ConfigureAwait(false);
        var random = CreateRandom(accountId);
        return Enumerable.Range(0, 3).Select(index =>
            new DailyMission
            {
                Title = $"{MissionVerbs[random.Next(MissionVerbs.Length)]} ({index + 1}/3)",
                Description = "Complete your daily objectives to earn XP.",
                Target = random.Next(15, 40),
                Progress = random.Next(0, 15),
                ExpiresAt = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(random.Next(24))
            }).ToList();
    }

    public async Task<IReadOnlyList<MatchSummary>> GetRecentMatchesAsync(Guid? accountId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(SimulatedDelay, cancellationToken).ConfigureAwait(false);
        var random = CreateRandom(accountId);
        return Enumerable.Range(0, 5).Select(index =>
            new MatchSummary
            {
                MatchId = Guid.NewGuid().ToString("N"),
                Map = Maps[random.Next(Maps.Length)],
                Mode = Modes[random.Next(Modes.Length)],
                Agent = Agents[random.Next(Agents.Length)],
                Result = random.NextDouble() > 0.45 ? "Victory" : "Defeat",
                RoundsWon = random.Next(13, 15),
                RoundsLost = random.Next(0, 13),
                CombatScore = Math.Round(random.NextDouble() * 350 + 150, 0),
                Kda = Math.Round(random.NextDouble() * 2.5 + 0.6, 2),
                CompletedAt = DateTimeOffset.UtcNow.AddHours(-index * 3 - random.Next(3))
            }).ToList();
    }

    public async Task<IReadOnlyList<StoreOffer>> GetStoreOffersAsync(Guid? accountId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(SimulatedDelay, cancellationToken).ConfigureAwait(false);
        var random = CreateRandom(accountId);
        return Enumerable.Range(0, 4).Select(index =>
            new StoreOffer
            {
                OfferId = Guid.NewGuid().ToString("N"),
                ItemName = $"Premium Skin {index + 1}",
                ItemType = "Weapon Skin",
                Currency = "VP",
                Cost = random.Next(875, 2175),
                ImageUrl = "https://assistsite.pages.dev/assets/placeholder.png",
                ExpiresAt = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(7)
            }).ToList();
    }

    public async Task<LiveGameInfo?> GetLiveGameAsync(Guid? accountId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(SimulatedDelay, cancellationToken).ConfigureAwait(false);
        var random = CreateRandom(accountId);
        if (random.NextDouble() < 0.35)
        {
            return null; // not currently in a match
        }

        var ally = GenerateTeam(random, "Ally");
        var enemy = GenerateTeam(random, "Enemy");
        return new LiveGameInfo
        {
            MatchId = Guid.NewGuid().ToString("N"),
            Map = Maps[random.Next(Maps.Length)],
            Mode = Modes[random.Next(Modes.Length)],
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-random.Next(2, 25)),
            RoundNumber = random.Next(3, 22),
            AllyTeam = ally,
            EnemyTeam = enemy
        };
    }

    public async Task<IReadOnlyList<NewsArticle>> GetNewsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(SimulatedDelay, cancellationToken).ConfigureAwait(false);
        var random = CreateRandom(null);
        return Enumerable.Range(0, 4).Select(index =>
            new NewsArticle
            {
                Title = $"{NewsTitles[random.Next(NewsTitles.Length)]} #{index + 1}",
                Summary = "Catch up with everything happening around Valorant.",
                Url = new Uri("https://playvalorant.com/en-us/news/"),
                PublishedAt = DateTimeOffset.UtcNow.AddDays(-index)
            }).ToList();
    }

    private static IReadOnlyList<PlayerPerformance> GenerateTeam(Random random, string prefix)
    {
        var players = new List<PlayerPerformance>();
        for (var i = 0; i < 5; i++)
        {
            players.Add(new PlayerPerformance
            {
                DisplayName = $"{prefix} Player {i + 1}",
                Agent = Agents[random.Next(Agents.Length)],
                Kills = random.Next(0, 25),
                Deaths = random.Next(0, 20),
                Assists = random.Next(0, 10),
                CombatScore = Math.Round(random.NextDouble() * 300 + 100, 0)
            });
        }

        return players;
    }

    private static Random CreateRandom(Guid? seed)
    {
        if (seed is null || seed == Guid.Empty)
        {
            return new Random();
        }

        var hash = HashCode.Combine(seed.Value);
        return new Random(hash);
    }
}
