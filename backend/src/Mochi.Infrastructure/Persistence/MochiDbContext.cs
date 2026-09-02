using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Mochi.Domain.Accounts;
using Mochi.Domain.Collection;
using Mochi.Domain.Goals;
using Mochi.Domain.Sites;

namespace Mochi.Infrastructure.Persistence;

/// <summary>EF Core context for Postgres. Schema per ADR 0003.</summary>
public sealed class MochiDbContext(DbContextOptions<MochiDbContext> options) : DbContext(options)
{
    /// <summary>Registered sites.</summary>
    public DbSet<Site> Sites => Set<Site>();

    /// <summary>Raw scrubbed events, 7-day lifetime.</summary>
    public DbSet<AnalyticsEvent> Events => Set<AnalyticsEvent>();

    /// <summary>Conversion goals.</summary>
    public DbSet<Goal> Goals => Set<Goal>();

    /// <summary>Dashboard accounts.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Dashboard sessions, token hashes only.</summary>
    public DbSet<Session> Sessions => Set<Session>();

    /// <summary>User-to-site memberships.</summary>
    public DbSet<SiteMembership> Memberships => Set<SiteMembership>();

    internal DbSet<SiteStatsRow> SiteStats => Set<SiteStatsRow>();
    internal DbSet<PageRow> PageRows => Set<PageRow>();
    internal DbSet<SourceRow> SourceRows => Set<SourceRow>();
    internal DbSet<GeoRow> GeoRows => Set<GeoRow>();
    internal DbSet<DeviceRow> DeviceRows => Set<DeviceRow>();
    internal DbSet<EventRow> EventRows => Set<EventRow>();

    private static readonly ValueConverter<SiteId, string> SiteIdConverter =
        new(id => id.Value, value => ParseSiteId(value));

    private static readonly ValueConverter<VisitorHash, long> VisitorHashConverter =
        new(h => unchecked((long)h.Value), v => VisitorHash.FromValue(unchecked((ulong)v)));

    private static SiteId ParseSiteId(string value)
        => SiteId.TryParse(value, out var id) ? id : throw new InvalidOperationException($"invalid site id in database: {value}");

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Site>(e =>
        {
            e.ToTable("sites");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasConversion(SiteIdConverter).HasMaxLength(8);
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Domain).HasColumnName("domain");
            e.Property(x => x.Timezone).HasColumnName("timezone");
            e.Property(x => x.Retention).HasColumnName("retention").HasConversion<short>();
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        b.Entity<AnalyticsEvent>(e =>
        {
            e.ToTable("events");
            e.Property<long>("Id").HasColumnName("id").ValueGeneratedOnAdd();
            e.HasKey("Id");
            e.Property(x => x.SiteId).HasColumnName("site_id").HasConversion(SiteIdConverter).HasMaxLength(8);
            e.Property(x => x.Visitor).HasColumnName("visitor_hash").HasConversion(VisitorHashConverter);
            e.Property(x => x.Type).HasColumnName("type").HasConversion<short>();
            e.Property(x => x.Path).HasColumnName("path");
            e.Property(x => x.EventName).HasColumnName("event_name");
            e.Property(x => x.ReferrerDomain).HasColumnName("referrer_domain");
            e.Property(x => x.Channel).HasColumnName("channel").HasConversion<short>();
            e.Property(x => x.Campaign).HasColumnName("campaign");
            e.Property(x => x.CountryCode).HasColumnName("country").HasMaxLength(2);
            e.Property(x => x.DeviceClass).HasColumnName("device_class").HasConversion<short>();
            e.Property(x => x.Browser).HasColumnName("browser");
            e.Property(x => x.Os).HasColumnName("os");
            e.Property(x => x.OccurredAt).HasColumnName("ts");
            e.HasIndex(x => new { x.SiteId, x.OccurredAt });
            e.HasOne<Site>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Goal>(e =>
        {
            e.ToTable("goals");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SiteId).HasColumnName("site_id").HasConversion(SiteIdConverter).HasMaxLength(8);
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Type).HasColumnName("type").HasConversion<short>();
            e.Property(x => x.Target).HasColumnName("target");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne<Site>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Email).HasColumnName("email");
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasColumnName("password_hash");
            e.Property(x => x.IsAdmin).HasColumnName("is_admin");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        b.Entity<Session>(e =>
        {
            e.ToTable("sessions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.TokenHash).HasColumnName("token_hash");
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.LastSeenAt).HasColumnName("last_seen_at");
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<SiteMembership>(e =>
        {
            e.ToTable("site_users");
            e.HasKey(x => new { x.UserId, x.SiteId });
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.SiteId).HasColumnName("site_id").HasConversion(SiteIdConverter).HasMaxLength(8);
            e.Property(x => x.Role).HasColumnName("role").HasConversion<short>();
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Site>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<SiteStatsRow>(e =>
        {
            e.ToTable("daily_site_stats");
            e.HasKey(x => new { x.SiteId, x.Date });
            MapRollupCommon(e.Property(x => x.SiteId));
            e.Property(x => x.Date).HasColumnName("date");
            e.Property(x => x.Visitors).HasColumnName("visitors");
            e.Property(x => x.Pageviews).HasColumnName("pageviews");
            e.Property(x => x.Sessions).HasColumnName("sessions");
            e.Property(x => x.BouncedSessions).HasColumnName("bounced_sessions");
            e.Property(x => x.TotalSessionDurationSec).HasColumnName("total_session_duration_sec");
        });

        b.Entity<PageRow>(e =>
        {
            e.ToTable("daily_pages");
            e.HasKey(x => new { x.SiteId, x.Date, x.Path });
            MapRollupCommon(e.Property(x => x.SiteId));
            e.Property(x => x.Date).HasColumnName("date");
            e.Property(x => x.Path).HasColumnName("path");
            e.Property(x => x.Visitors).HasColumnName("visitors");
            e.Property(x => x.Pageviews).HasColumnName("pageviews");
            e.Property(x => x.Entries).HasColumnName("entries");
            e.Property(x => x.Exits).HasColumnName("exits");
            e.Property(x => x.BouncedSessions).HasColumnName("bounced_sessions");
            e.Property(x => x.TotalDurationSec).HasColumnName("total_duration_sec");
        });

        b.Entity<SourceRow>(e =>
        {
            e.ToTable("daily_sources");
            e.HasKey(x => new { x.SiteId, x.Date, x.Channel, x.ReferrerDomain, x.Campaign });
            MapRollupCommon(e.Property(x => x.SiteId));
            e.Property(x => x.Date).HasColumnName("date");
            e.Property(x => x.Channel).HasColumnName("channel");
            e.Property(x => x.ReferrerDomain).HasColumnName("referrer_domain");
            e.Property(x => x.Campaign).HasColumnName("campaign");
            e.Property(x => x.Visitors).HasColumnName("visitors");
            e.Property(x => x.Pageviews).HasColumnName("pageviews");
            e.Property(x => x.BouncedSessions).HasColumnName("bounced_sessions");
        });

        b.Entity<GeoRow>(e =>
        {
            e.ToTable("daily_geo");
            e.HasKey(x => new { x.SiteId, x.Date, x.Country });
            MapRollupCommon(e.Property(x => x.SiteId));
            e.Property(x => x.Date).HasColumnName("date");
            e.Property(x => x.Country).HasColumnName("country").HasMaxLength(2);
            e.Property(x => x.Visitors).HasColumnName("visitors");
        });

        b.Entity<DeviceRow>(e =>
        {
            e.ToTable("daily_devices");
            e.HasKey(x => new { x.SiteId, x.Date, x.DeviceClass, x.Browser, x.Os });
            MapRollupCommon(e.Property(x => x.SiteId));
            e.Property(x => x.Date).HasColumnName("date");
            e.Property(x => x.DeviceClass).HasColumnName("device_class");
            e.Property(x => x.Browser).HasColumnName("browser");
            e.Property(x => x.Os).HasColumnName("os");
            e.Property(x => x.Visitors).HasColumnName("visitors");
        });

        b.Entity<EventRow>(e =>
        {
            e.ToTable("daily_events");
            e.HasKey(x => new { x.SiteId, x.Date, x.EventName, x.Path, x.Channel });
            MapRollupCommon(e.Property(x => x.SiteId));
            e.Property(x => x.Date).HasColumnName("date");
            e.Property(x => x.EventName).HasColumnName("event_name");
            e.Property(x => x.Path).HasColumnName("path");
            e.Property(x => x.Channel).HasColumnName("channel");
            e.Property(x => x.Total).HasColumnName("total");
            e.Property(x => x.UniqueVisitors).HasColumnName("unique_visitors");
        });
    }

    // Rollup tables carry site_id as a plain column, no FK to sites. Site
    // deletion purges them through IRollupStore.PurgeSiteAsync instead of a
    // database cascade.
    private static void MapRollupCommon(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<string> siteId)
        => siteId.HasColumnName("site_id").HasMaxLength(8);
}

