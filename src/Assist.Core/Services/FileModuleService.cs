using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Assist.Core.Abstractions;
using Assist.Core.Infrastructure;
using Assist.Core.Models;

namespace Assist.Core.Services;

public sealed class FileModuleService : IModuleService
{
    private const string FileName = "modules.json";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<IReadOnlyList<ModuleDefinition>> GetModulesAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var store = await ReadStoreAsync(cancellationToken).ConfigureAwait(false);
            await EnsurePersistedAsync(store, cancellationToken).ConfigureAwait(false);
            return store.Modules.OrderBy(m => m.Name).ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SetModuleStateAsync(Guid moduleId, bool enabled, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var store = await ReadStoreAsync(cancellationToken).ConfigureAwait(false);
            var index = store.Modules.FindIndex(m => m.Id == moduleId);
            if (index < 0)
            {
                throw new InvalidOperationException($"Module with id {moduleId} was not found.");
            }

            store.Modules[index].IsEnabled = enabled;
            await WriteStoreAsync(store, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static async Task<ModuleStoreModel> ReadStoreAsync(CancellationToken cancellationToken)
    {
        var path = AppDataPaths.GetFilePath(FileName);
        if (!File.Exists(path))
        {
            return new ModuleStoreModel
            {
                Modules = GetDefaultModules().ToList()
            };
        }

        await using var stream = File.OpenRead(path);
        var store = await JsonSerializer.DeserializeAsync<ModuleStoreModel>(stream, SerializerOptions, cancellationToken)
                    .ConfigureAwait(false);
        if (store is null || store.Modules.Count == 0)
        {
            return new ModuleStoreModel
            {
                Modules = GetDefaultModules().ToList()
            };
        }

        return store;
    }

    private static async Task EnsurePersistedAsync(ModuleStoreModel store, CancellationToken cancellationToken)
    {
        var path = AppDataPaths.GetFilePath(FileName);
        if (!File.Exists(path))
        {
            await WriteStoreAsync(store, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task WriteStoreAsync(ModuleStoreModel store, CancellationToken cancellationToken)
    {
        var path = AppDataPaths.GetFilePath(FileName);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, store, SerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    private static IEnumerable<ModuleDefinition> GetDefaultModules() => new[]
    {
        new ModuleDefinition
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Discord Rich Presence",
            Description = "Shares your Valorant status with your Discord friends.",
            Category = ModuleCategory.Social
        },
        new ModuleDefinition
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "Auto Launch",
            Description = "Launch Valorant immediately when you select an account.",
            Category = ModuleCategory.Utility
        },
        new ModuleDefinition
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Name = "Performance Tracking",
            Description = "Keep a rolling log of combat score and KDA across matches.",
            Category = ModuleCategory.Gameplay
        },
        new ModuleDefinition
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Name = "Dodge List",
            Description = "Keep track of toxic players to avoid future matches.",
            Category = ModuleCategory.Utility
        }
    };

    private sealed class ModuleStoreModel
    {
        public List<ModuleDefinition> Modules { get; set; } = new();
    }
}
