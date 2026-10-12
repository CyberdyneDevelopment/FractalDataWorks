using System;
using System.Threading;
using Fdw.Services.Data.Limits;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Limits.Tests;

public sealed class TokenBucketTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Limits")]
    public void TryConsume_WithFullBucket_ReturnsTrue()
    {
        var bucket = new TokenBucket(10, 10);
        bucket.TryConsume().ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Limits")]
    public void TryConsume_ExceedsCapacity_ReturnsFalse()
    {
        // Create bucket with capacity of 3
        var bucket = new TokenBucket(10, 3);

        // Drain all 3 tokens
        bucket.TryConsume().ShouldBeTrue();
        bucket.TryConsume().ShouldBeTrue();
        bucket.TryConsume().ShouldBeTrue();

        // Next consume should fail — bucket is empty
        bucket.TryConsume().ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Limits")]
    public void CurrentTokens_AfterConsume_ReflectsDecrease()
    {
        var bucket = new TokenBucket(10, 5);
        bucket.TryConsume(); // consume 1

        // After refill of ~0s, should be close to 4.x (may have slight refill)
        bucket.CurrentTokens.ShouldBeLessThan(5.0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Limits")]
    public void TryConsume_AfterDelay_RefillsTokens()
    {
        // 100 tokens/second, max 2
        var bucket = new TokenBucket(100, 2);

        // Drain the bucket
        bucket.TryConsume().ShouldBeTrue();
        bucket.TryConsume().ShouldBeTrue();
        bucket.TryConsume().ShouldBeFalse();

        // Wait 50ms (at 100 tokens/sec = 5 tokens should refill, capped at 2)
        Thread.Sleep(50);

        // Should be able to consume again after refill
        bucket.TryConsume().ShouldBeTrue();
    }
}
