using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Mochi.Infrastructure.Persistence;

/// <summary>Design-time factory so dotnet-ef can create migrations without a running app.</summary>
public sealed class MochiDbContextFactory : IDesignTimeDbContextFactory<MochiDbContext>
{
    /// <inheritdoc />
    public MochiDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MochiDbContext>()
            .UseNpgsql("Host=localhost;Database=mochi;Username=mochi;Password=mochi")
            .Options;
        return new MochiDbContext(options);
    }
}
