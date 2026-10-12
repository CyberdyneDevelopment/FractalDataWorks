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
}
