using Fdw;
using Fdw.Services;
using Fdw.Services.EtlMappers;
namespace Fdw.Services.EtlMappers.Pooled.Tests;

public sealed class DictionaryPoolTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void RentReturnsNewDictionaryWhenPoolIsEmpty()
    {
        // Arrange
        var pool = new DictionaryPool();

        // Act
        var dict = pool.Rent(10);

        // Assert
        dict.ShouldNotBeNull();
        dict.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void RentReturnsClearedDictionaryFromPool()
    {
        // Arrange
        var pool = new DictionaryPool();
        var dict = pool.Rent(5);
        dict["key"] = "value";
        pool.Return(dict);

        // Act
        var reused = pool.Rent(5);

        // Assert
        reused.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ReturnAddsDictionaryToPool()
    {
        // Arrange
        var pool = new DictionaryPool();
        var dict = pool.Rent(5);

        // Act
        pool.Return(dict);

        // Assert
        pool.CurrentPoolSize.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ReturnIgnoresNonConcreteDictionary()
    {
        // Arrange
        var pool = new DictionaryPool();
        var mockDict = new Mock<IDictionary<string, object?>>();
        mockDict.Setup(d => d.Count).Returns(5);

        // Act
        pool.Return(mockDict.Object);

        // Assert
        pool.CurrentPoolSize.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ReturnIgnoresOversizedDictionary()
    {
        // Arrange
        var pool = new DictionaryPool(maxPoolSize: 100, maxDictionarySize: 2);
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3
        };

        // Act
        pool.Return(dict);

        // Assert
        pool.CurrentPoolSize.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ReturnDoesNotExceedMaxPoolSize()
    {
        // Arrange
        var pool = new DictionaryPool(maxPoolSize: 2, maxDictionarySize: 100);

        // Act
        for (int i = 0; i < 5; i++)
        {
            var dict = pool.Rent(5);
            pool.Return(dict);
        }

        // Assert
        pool.CurrentPoolSize.ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ClearRemovesAllDictionariesFromPool()
    {
        // Arrange
        var pool = new DictionaryPool();
        for (int i = 0; i < 5; i++)
        {
            var dict = pool.Rent(5);
            pool.Return(dict);
        }
        pool.CurrentPoolSize.ShouldBeGreaterThan(0);

        // Act
        pool.Clear();

        // Assert
        pool.CurrentPoolSize.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CurrentPoolSizeReflectsPoolState()
    {
        // Arrange
        var pool = new DictionaryPool();

        // Assert initial
        pool.CurrentPoolSize.ShouldBe(0);

        // Add one
        var dict1 = pool.Rent(5);
        pool.Return(dict1);
        pool.CurrentPoolSize.ShouldBe(1);

        // Rent one (removes from pool)
        pool.Rent(5);
        pool.CurrentPoolSize.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void DefaultConstructorUsesDefaultPoolAndDictionarySizes()
    {
        // Arrange & Act
        var pool = new DictionaryPool();

        // Assert - should allow returning many distinct dictionaries (default max is 1000)
        var dicts = new List<IDictionary<string, object?>>();
        for (int i = 0; i < 10; i++)
        {
            dicts.Add(pool.Rent(5));
        }
        foreach (var dict in dicts)
        {
            pool.Return(dict);
        }
        pool.CurrentPoolSize.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void RentedDictionaryUsesCaseInsensitiveComparer()
    {
        // Arrange
        var pool = new DictionaryPool();

        // Act
        var dict = pool.Rent(5);
        dict["Key"] = "value";

        // Assert - new dictionaries should use OrdinalIgnoreCase comparer
        dict["key"].ShouldBe("value");
        dict["KEY"].ShouldBe("value");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ReturnDropsDictionaryWhenPoolIsFull()
    {
        // Arrange - create a pool with maxPoolSize=2, then fill it to capacity
        var pool = new DictionaryPool(maxPoolSize: 2, maxDictionarySize: 100);

        // Rent 3 separate dictionaries
        var dict1 = pool.Rent(5);
        var dict2 = pool.Rent(5);
        var dict3 = pool.Rent(5);

        // Return first 2 (fills the pool to capacity)
        pool.Return(dict1);
        pool.Return(dict2);
        pool.CurrentPoolSize.ShouldBe(2);

        // Act - return a 3rd when pool is full (should be dropped)
        pool.Return(dict3);

        // Assert - pool size should remain at 2 (line 58: pool.Count >= maxPoolSize)
        pool.CurrentPoolSize.ShouldBe(2);
    }
}
