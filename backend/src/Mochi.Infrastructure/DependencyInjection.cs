using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mochi.Application.Abstractions;
using Mochi.Application.Collect;
using Mochi.Application.Rollups;
using Mochi.Application.Sites;
using Mochi.Application.Stats;
using Mochi.Infrastructure.Auth;
using Mochi.Infrastructure.Collection;
using Mochi.Infrastructure.InMemory;
using Mochi.Infrastructure.Persistence;
using Mochi.Infrastructure.Rollups;
using Mochi.Infrastructure.Time;

namespace Mochi.Infrastructure;

/// <summary>Composition root helpers.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers application handlers, the rollup job and the adapter set.
    /// Storage is Postgres when ConnectionStrings:Mochi is configured,
    /// in-memory otherwise. Geo lookup needs Mochi:GeoIpDatabase pointing at a
    /// GeoLite2-Country.mmdb file, else countries stay null.
    /// </summary>
    public static IServiceCollection AddMochi(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IDailySaltProvider, RotatingDailySaltProvider>();
        services.AddSingleton<IUserAgentParser, UaParserUserAgentParser>();

        var geoDb = config["Mochi:GeoIpDatabase"];
        if (string.IsNullOrWhiteSpace(geoDb))
            services.AddSingleton<IGeoLocator, NullGeoLocator>();
        else
            services.AddSingleton<IGeoLocator>(_ => new MaxMindGeoLocator(geoDb));

        var connectionString = config.GetConnectionString("Mochi");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<ISiteRepository, InMemorySiteRepository>();
            services.AddSingleton<IGoalRepository, InMemoryGoalRepository>();
            services.AddSingleton<IUserRepository, InMemoryUserRepository>();
            services.AddSingleton<ISessionStore, InMemorySessionStore>();
            services.AddSingleton<IMembershipRepository, InMemoryMembershipRepository>();
            services.AddSingleton<IAnalyticsEventStore, InMemoryAnalyticsEventStore>();
            services.AddSingleton<InMemoryRollupStore>();
            services.AddSingleton<IRollupStore>(sp => sp.GetRequiredService<InMemoryRollupStore>());
            services.AddSingleton<IRollupReader, InMemoryRollupReader>();
        }
        else
        {
            services.AddDbContext<MochiDbContext>(o => o.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));
            services.AddScoped<ISiteRepository, EfSiteRepository>();
            services.AddScoped<IGoalRepository, EfGoalRepository>();
            services.AddScoped<IUserRepository, EfUserRepository>();
            services.AddScoped<ISessionStore, EfSessionStore>();
            services.AddScoped<IMembershipRepository, EfMembershipRepository>();
            services.AddScoped<IAnalyticsEventStore, EfAnalyticsEventStore>();
            services.AddScoped<IRollupStore, EfRollupStore>();
            services.AddScoped<IRollupReader, EfRollupReader>();
        }

        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<ISetupCodeProvider, SetupCodeProvider>();
        services.AddScoped<Application.Auth.AuthService>();
        services.AddScoped<CollectHandler>();
        services.AddScoped<RegisterSiteHandler>();
        services.AddScoped<RollupJob>();
        services.AddScoped<StatsService>();
        services.AddHostedService<DailyRollupHostedService>();
        return services;
    }
}
