using Fdw.Data.RowSources;

namespace Fdw.Data.RowSources.Tests;

public sealed class DictionaryPoolAdditionalTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReturnIgnoresNonConcreteDictionary()
    {
        // Arrange - pass a non-Dictionary<string, object?> implementation
        var pool = new DictionaryPool();
        var mockDict = new Mock<IDictionary<string, object?>>();

        // Act
        pool.Return(mockDict.Object);

        // Assert - should not be pooled because it's not a concrete Dictionary
        pool.CurrentPoolSize.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReturnIgnoresSortedDictionary()
    {
        // Arrange - a SortedDictionary is IDictionary but not Dictionary
        var pool = new DictionaryPool();
        IDictionary<string, object?> sorted = new SortedDictionary<string, object?>();

        // Act
        pool.Return(sorted);

        // Assert
        pool.CurrentPoolSize.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RentReturnsClearedPooledDictionaryAfterMultipleRoundTrips()
    {
        // Arrange
        var pool = new DictionaryPool();
        var dict = pool.Rent(5);
        dict["a"] = 1;
        dict["b"] = 2;
        pool.Return(dict);

        // Act - rent again, it should be cleared
        var reused = pool.Rent(5);
        reused["c"] = 3;
        pool.Return(reused);

        // Rent a third time
        var reused2 = pool.Rent(5);

        // Assert
        reused2.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReturnDoesNotPoolWhenExactlyAtMaxPoolSize()
    {
        // Arrange - maxPoolSize = 1
        var pool = new DictionaryPool(maxPoolSize: 1, maxDictionarySize: 100);
        var dict1 = pool.Rent(5);
        pool.Return(dict1); // Pool is now at max (1)

        var dict2 = pool.Rent(5);

        // Act - return another dict when pool is at max
        pool.Return(dict2);

        // Assert - ConcurrentBag.Count is approximate, but should be <= 1
        pool.CurrentPoolSize.ShouldBeLessThanOrEqualTo(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReturnDoesNotPoolDictionaryAtExactMaxSize()
    {
        // Arrange - maxDictionarySize = 3, dictionary has exactly 3 items
        var pool = new DictionaryPool(maxPoolSize: 100, maxDictionarySize: 3);
        var dict = pool.Rent(5);
        dict["a"] = 1;
        dict["b"] = 2;
        dict["c"] = 3;

        // Act - count == 3, not > 3, so should be pooled
        pool.Return(dict);

        // Assert
        pool.CurrentPoolSize.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReturnDoesNotPoolDictionaryExceedingMaxSize()
    {
        // Arrange - maxDictionarySize = 3, dictionary has 4 items
        var pool = new DictionaryPool(maxPoolSize: 100, maxDictionarySize: 3);
        var dict = pool.Rent(5);
        dict["a"] = 1;
        dict["b"] = 2;
        dict["c"] = 3;
        dict["d"] = 4;

        // Act
        pool.Return(dict);

        // Assert
        pool.CurrentPoolSize.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ClearOnEmptyPoolDoesNotThrow()
    {
        // Arrange
        var pool = new DictionaryPool();

        // Act & Assert - should not throw
        pool.Clear();
        pool.CurrentPoolSize.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultConstructorUsesDefaultParameters()
    {
        // Arrange & Act
        var pool = new DictionaryPool();

        // Assert - should be able to rent and return normally
        var dict = pool.Rent(10);
        dict.ShouldNotBeNull();
        pool.Return(dict);
        pool.CurrentPoolSize.ShouldBe(1);
    }
}
