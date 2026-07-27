using Lafie.Caching;
using Xunit;

namespace Lafie.Framework.Tests.Caching;

public class CacheKeysTests
{
    [Fact]
    public void ForEntity_formats_key()
    {
        Assert.Equal("patient:42", CacheKeys.ForEntity("patient", 42));
    }

    [Fact]
    public void ForList_formats_key()
    {
        Assert.Equal("patient:list:active", CacheKeys.ForList("patient", "active"));
    }
}
