using Mochi.Domain.Sites;

namespace Mochi.Domain.Tests;

public class SiteIdTests
{
    [Fact]
    public void New_produces_prefixed_eight_char_id()
    {
        var id = SiteId.New();

        Assert.Equal(8, id.Value.Length);
        Assert.StartsWith("MC-", id.Value);
    }

    [Fact]
    public void New_roundtrips_through_TryParse()
    {
        var id = SiteId.New();

        Assert.True(SiteId.TryParse(id.Value, out var parsed));
        Assert.Equal(id, parsed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("MC-7F3K")]
    [InlineData("MC-7F3K22")]
    [InlineData("XX-7F3K2")]
    [InlineData("MC-7f3k2")]
    [InlineData("MC-7F3KI")]
    public void TryParse_rejects_invalid_input(string? input)
    {
        Assert.False(SiteId.TryParse(input, out _));
    }
}
