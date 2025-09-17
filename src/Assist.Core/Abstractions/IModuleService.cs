using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Assist.Core.Models;

namespace Assist.Core.Abstractions;

public interface IModuleService
{
    Task<IReadOnlyList<ModuleDefinition>> GetModulesAsync(CancellationToken cancellationToken = default);

    Task SetModuleStateAsync(Guid moduleId, bool enabled, CancellationToken cancellationToken = default);
}
