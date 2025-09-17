using Assist.Core.Abstractions;
using Assist.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Assist.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAssistCore(this IServiceCollection services)
    {
        services.AddSingleton<IAccountService, FileAccountService>();
        services.AddSingleton<IModuleService, FileModuleService>();
        services.AddSingleton<IValorantDataService, MockValorantDataService>();
        return services;
    }
}
