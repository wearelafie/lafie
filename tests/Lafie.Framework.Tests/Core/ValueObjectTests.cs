using Lafie.Core.Domain;
using Xunit;

namespace Lafie.Framework.Tests.Core;

public class ValueObjectTests
{
    private sealed class Address(string city, string zip) : ValueObject
    {
        public string City { get; } = city;
        public string Zip { get; } = zip;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return City;
            yield return Zip;
        }
    }

    [Fact]
    public void Same_components_are_equal()
    {
        Assert.Equal(new Address("Lomé", "01"), new Address("Lomé", "01"));
        Assert.Equal(new Address("Lomé", "01").GetHashCode(), new Address("Lomé", "01").GetHashCode());
    }

    [Fact]
    public void Different_components_are_not_equal()
    {
        Assert.NotEqual(new Address("Lomé", "01"), new Address("Kara", "01"));
    }
}
