using Mochi.Domain.Collection;
using Mochi.Domain.Sites;

namespace Mochi.Domain.Tests;

public class VisitorHashTests
{
    private static readonly byte[] SaltA = [1, 2, 3, 4];
    private static readonly byte[] SaltB = [5, 6, 7, 8];

    private static SiteId Site()
    {
        Assert.True(SiteId.TryParse("MC-7F3K2", out var id));
        return id;
    }

    [Fact]
    public void Same_inputs_same_hash()
    {
        var a = VisitorHash.Compute(SaltA, Site(), "203.0.113.7", "Mozilla/5.0");
        var b = VisitorHash.Compute(SaltA, Site(), "203.0.113.7", "Mozilla/5.0");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Different_salt_different_hash()
    {
        var a = VisitorHash.Compute(SaltA, Site(), "203.0.113.7", "Mozilla/5.0");
        var b = VisitorHash.Compute(SaltB, Site(), "203.0.113.7", "Mozilla/5.0");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Different_ip_different_hash()
    {
        var a = VisitorHash.Compute(SaltA, Site(), "203.0.113.7", "Mozilla/5.0");
        var b = VisitorHash.Compute(SaltA, Site(), "203.0.113.8", "Mozilla/5.0");

        Assert.NotEqual(a, b);
    }
}
