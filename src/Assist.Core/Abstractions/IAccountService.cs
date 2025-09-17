using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Assist.Core.Models;

namespace Assist.Core.Abstractions;

public interface IAccountService
{
    event EventHandler? AccountsChanged;

    Task<IReadOnlyList<ValorantAccount>> GetAccountsAsync(CancellationToken cancellationToken = default);

    Task<ValorantAccount?> GetActiveAccountAsync(CancellationToken cancellationToken = default);

    Task<ValorantAccount> AddOrUpdateAsync(ValorantAccount account, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task SetActiveAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
}
