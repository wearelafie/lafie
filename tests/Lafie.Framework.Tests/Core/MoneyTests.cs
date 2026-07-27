using Lafie.Core.Domain;
using Xunit;

namespace Lafie.Framework.Tests.Core;

public class MoneyTests
{
    [Fact]
    public void Add_same_currency()
    {
        var sum = new Money(10m, "XOF").Add(new Money(5m, "XOF"));
        Assert.Equal(15m, sum.Amount);
        Assert.Equal("XOF", sum.Currency);
    }

    [Fact]
    public void Subtract_and_multiply()
    {
        Assert.Equal(5m, new Money(10m, "XOF").Subtract(new Money(5m, "XOF")).Amount);
        Assert.Equal(20m, new Money(10m, "XOF").Multiply(2m).Amount);
    }

    [Fact]
    public void Currency_is_normalized_uppercase()
    {
        Assert.Equal("EUR", new Money(1m, "eur").Currency);
    }

    [Fact]
    public void Different_currencies_throws()
    {
        Assert.Throws<InvalidOperationException>(() => new Money(1m, "XOF").Add(new Money(1m, "EUR")));
    }

    [Fact]
    public void Zero_defaults_to_xof()
    {
        var zero = Money.Zero();
        Assert.Equal(0m, zero.Amount);
        Assert.Equal("XOF", zero.Currency);
    }
}
