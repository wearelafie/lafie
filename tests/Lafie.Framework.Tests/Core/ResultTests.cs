using Lafie.Core.Common;
using Xunit;

namespace Lafie.Framework.Tests.Core;

public class ResultTests
{
    [Fact]
    public void Success_is_success_without_error()
    {
        var r = Result.Success();
        Assert.True(r.IsSuccess);
        Assert.False(r.IsFailure);
        Assert.Null(r.Error);
    }

    [Fact]
    public void Failure_is_failure_with_error()
    {
        var r = Result.Failure("boom");
        Assert.True(r.IsFailure);
        Assert.Equal("boom", r.Error);
    }

    [Fact]
    public void Generic_success_exposes_value()
    {
        var r = Result.Success(42);
        Assert.True(r.IsSuccess);
        Assert.Equal(42, r.Value);
    }

    [Fact]
    public void Generic_failure_throws_on_value()
    {
        var r = Result.Failure<int>("nope");
        Assert.Throws<InvalidOperationException>(() => r.Value);
    }
}
