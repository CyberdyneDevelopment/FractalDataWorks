using Fdw.Data.RowSources;

namespace Fdw.Data.RowSources.Tests;

/// <summary>
/// Tests for the DictionaryPool.
/// </summary>
public class DictionaryPoolTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
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
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReturnAndRentReusesDictionary()
    {
        // Arrange
        var pool = new DictionaryPool();
        var dict1 = pool.Rent(10);
        dict1["key"] = "value";
        pool.Return(dict1);

        // Act
        var dict2 = pool.Rent(10);

        // Assert
        dict2.Count.ShouldBe(0); // Should be cleared
        pool.CurrentPoolSize.ShouldBe(0); // Should be taken from pool
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReturnClearsDictionary()
    {
        // Arrange
        var pool = new DictionaryPool();
        var dict = pool.Rent(10);
        dict["key1"] = "value1";
        dict["key2"] = "value2";

        // Act
        pool.Return(dict);
        var reused = pool.Rent(10);

        // Assert
        reused.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReturnDoesNotPoolOversizedDictionaries()
    {
        // Arrange
        var pool = new DictionaryPool(maxPoolSize: 100, maxDictionarySize: 5);
        var dict = pool.Rent(10);

        // Add more items than maxDictionarySize
        for (int i = 0; i < 10; i++)
        {
            dict[$"key{i}"] = i;
        }

        // Act
        pool.Return(dict);

        // Assert
        pool.CurrentPoolSize.ShouldBe(0); // Should not be pooled
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReturnDoesNotExceedMaxPoolSize()
    {
        // Arrange
        var pool = new DictionaryPool(maxPoolSize: 2, maxDictionarySize: 100);

        // Act - return more dictionaries than maxPoolSize
        for (int i = 0; i < 5; i++)
        {
            var dict = pool.Rent(10);
            pool.Return(dict);
        }

        // Assert
        pool.CurrentPoolSize.ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ClearEmptiesPool()
    {
        // Arrange
        var pool = new DictionaryPool();
        pool.Return(pool.Rent(10));
        pool.Return(pool.Rent(10));
        pool.Return(pool.Rent(10));

        // Act
        pool.Clear();

        // Assert
        pool.CurrentPoolSize.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RentedDictionaryIsCaseInsensitive()
    {
        // Arrange
        var pool = new DictionaryPool();

        // Act
        var dict = pool.Rent(10);
        dict["TestKey"] = "value";

        // Assert
        dict["testkey"].ShouldBe("value");
        dict["TESTKEY"].ShouldBe("value");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ReturnAddsDictionaryToPool()
    {
        // Arrange
        var pool = new DictionaryPool();
        var dict = pool.Rent(10);

        // Act
        pool.Return(dict);

        // Assert
        pool.CurrentPoolSize.ShouldBe(1);
    }
}
