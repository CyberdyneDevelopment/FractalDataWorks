using Fdw.Services.RateLimiting.Abstractions;

namespace Fdw.Services.RateLimiting.Abstractions.Tests;

/// <summary>
/// Tests for RateLimitAlgorithms TypeCollection.
/// Verifies all algorithm options are defined and have expected characteristics.
/// </summary>
public class RateLimitAlgorithmTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void FixedWindowHasId1()
    {
        // Assert
        RateLimitAlgorithms.FixedWindow.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SlidingWindowHasId2()
    {
        // Assert
        RateLimitAlgorithms.SlidingWindow.Id.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void TokenBucketHasId3()
    {
        // Assert
        RateLimitAlgorithms.TokenBucket.Id.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConcurrencyHasId4()
    {
        // Assert
        RateLimitAlgorithms.Concurrency.Id.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllAlgorithmsAreDefined()
    {
        // Act
        var all = RateLimitAlgorithms.All();

        // Assert
        all.ShouldContain(a => a.Name == "FixedWindow");
        all.ShouldContain(a => a.Name == "SlidingWindow");
        all.ShouldContain(a => a.Name == "TokenBucket");
        all.ShouldContain(a => a.Name == "Concurrency");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CollectionHasExactlyFourValues()
    {
        // Act
        var all = RateLimitAlgorithms.All();

        // Assert
        all.Count.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void FixedWindowHasCorrectName()
    {
        // Assert
        RateLimitAlgorithms.FixedWindow.Name.ShouldBe("FixedWindow");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SlidingWindowHasCorrectName()
    {
        // Assert
        RateLimitAlgorithms.SlidingWindow.Name.ShouldBe("SlidingWindow");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void TokenBucketHasCorrectName()
    {
        // Assert
        RateLimitAlgorithms.TokenBucket.Name.ShouldBe("TokenBucket");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConcurrencyHasCorrectName()
    {
        // Assert
        RateLimitAlgorithms.Concurrency.Name.ShouldBe("Concurrency");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameFixedWindowReturnsCorrectOption()
    {
        // Act
        var algorithm = RateLimitAlgorithms.ByName("FixedWindow");

        // Assert
        algorithm.Id.ShouldBe(1);
        algorithm.Name.ShouldBe("FixedWindow");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameSlidingWindowReturnsCorrectOption()
    {
        // Act
        var algorithm = RateLimitAlgorithms.ByName("SlidingWindow");

        // Assert
        algorithm.Name.ShouldBe("SlidingWindow");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameTokenBucketReturnsCorrectOption()
    {
        // Act
        var algorithm = RateLimitAlgorithms.ByName("TokenBucket");

        // Assert
        algorithm.Name.ShouldBe("TokenBucket");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameConcurrencyReturnsCorrectOption()
    {
        // Act
        var algorithm = RateLimitAlgorithms.ByName("Concurrency");

        // Assert
        algorithm.Name.ShouldBe("Concurrency");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ValuesHaveUniqueIds()
    {
        // Act
        var all = RateLimitAlgorithms.All();
        var uniqueIds = all.Select(a => a.Id).Distinct().Count();

        // Assert
        uniqueIds.ShouldBe(all.Count);
    }
}
