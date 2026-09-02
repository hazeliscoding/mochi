using Microsoft.Extensions.DependencyInjection;
using Mochi.Application.Abstractions;
using Mochi.Application.Collect;
using Mochi.Application.Sites;
using Mochi.Infrastructure.Collection;
using Mochi.Infrastructure.InMemory;
using Mochi.Infrastructure.Time;

namespace Mochi.Infrastructure;

/// <summary>Composition root helpers.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the application handlers and the current adapter set.
    /// Storage adapters are in-memory until Postgres lands in v0.2.
    /// </summary>
    public static IServiceCollection AddMochi(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IDailySaltProvider, RotatingDailySaltProvider>();
        services.AddSingleton<IUserAgentParser, NaiveUserAgentParser>();
        services.AddSingleton<IGeoLocator, NullGeoLocator>();
        services.AddSingleton<ISiteRepository, InMemorySiteRepository>();
        services.AddSingleton<IAnalyticsEventStore, InMemoryAnalyticsEventStore>();

        services.AddScoped<CollectHandler>();
        services.AddScoped<RegisterSiteHandler>();
        return services;
    }
}
