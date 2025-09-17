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

public sealed class FileAccountService : IAccountService
{
    private const string FileName = "accounts.json";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);

    public event EventHandler? AccountsChanged;

    public async Task<IReadOnlyList<ValorantAccount>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var store = await ReadStoreAsync(cancellationToken).ConfigureAwait(false);
            return store.Accounts.OrderByDescending(a => a.LastUsedAt ?? a.AddedAt).ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ValorantAccount?> GetActiveAccountAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var store = await ReadStoreAsync(cancellationToken).ConfigureAwait(false);
            if (store.ActiveAccountId is null)
            {
                return null;
            }

            return store.Accounts.FirstOrDefault(a => a.Id == store.ActiveAccountId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ValorantAccount> AddOrUpdateAsync(ValorantAccount account, CancellationToken cancellationToken = default)
    {
        if (account is null)
        {
            throw new ArgumentNullException(nameof(account));
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var store = await ReadStoreAsync(cancellationToken).ConfigureAwait(false);
            var existingIndex = store.Accounts.FindIndex(a => a.Id == account.Id);
            var normalizedAccount = NormalizeAccount(account);

            if (existingIndex >= 0)
            {
                store.Accounts[existingIndex] = normalizedAccount;
            }
            else
            {
                store.Accounts.Add(normalizedAccount);
            }

            await WriteStoreAsync(store, cancellationToken).ConfigureAwait(false);
            AccountsChanged?.Invoke(this, EventArgs.Empty);
            return normalizedAccount;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RemoveAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var store = await ReadStoreAsync(cancellationToken).ConfigureAwait(false);
            var removed = store.Accounts.RemoveAll(a => a.Id == accountId);
            if (removed == 0)
            {
                return;
            }

            if (store.ActiveAccountId == accountId)
            {
                store.ActiveAccountId = store.Accounts.FirstOrDefault()?.Id;
            }

            await WriteStoreAsync(store, cancellationToken).ConfigureAwait(false);
            AccountsChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SetActiveAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var store = await ReadStoreAsync(cancellationToken).ConfigureAwait(false);
            if (store.ActiveAccountId == accountId)
            {
                return;
            }

            if (store.Accounts.All(a => a.Id != accountId))
            {
                throw new InvalidOperationException($"Account with id {accountId} does not exist.");
            }

            store.ActiveAccountId = accountId;
            await WriteStoreAsync(store, cancellationToken).ConfigureAwait(false);
            AccountsChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static ValorantAccount NormalizeAccount(ValorantAccount account)
    {
        var id = account.Id == Guid.Empty ? Guid.NewGuid() : account.Id;
        var addedAt = account.AddedAt == default ? DateTimeOffset.UtcNow : account.AddedAt;
        return new ValorantAccount
        {
            Id = id,
            DisplayName = account.DisplayName,
            TagLine = account.TagLine,
            Region = account.Region,
            AddedAt = addedAt,
            LastUsedAt = account.LastUsedAt,
            Notes = account.Notes,
            RequiresSecondFactor = account.RequiresSecondFactor
        };
    }

    private static async Task<AccountStoreModel> ReadStoreAsync(CancellationToken cancellationToken)
    {
        var path = AppDataPaths.GetFilePath(FileName);
        if (!File.Exists(path))
        {
            return new AccountStoreModel();
        }

        await using var stream = File.OpenRead(path);
        var store = await JsonSerializer.DeserializeAsync<AccountStoreModel>(stream, SerializerOptions, cancellationToken)
                    .ConfigureAwait(false);
        return store ?? new AccountStoreModel();
    }

    private static async Task WriteStoreAsync(AccountStoreModel store, CancellationToken cancellationToken)
    {
        var path = AppDataPaths.GetFilePath(FileName);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, store, SerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    private sealed class AccountStoreModel
    {
        public List<ValorantAccount> Accounts { get; set; } = new();

        public Guid? ActiveAccountId { get; set; }
    }
}
