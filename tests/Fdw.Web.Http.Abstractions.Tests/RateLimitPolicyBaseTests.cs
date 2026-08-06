using Fdw.Web.Http.Abstractions.Policies;

namespace Fdw.Web.Http.Abstractions.Tests;

/// <summary>
/// Tests for <see cref="RateLimitPolicyBase"/> properties and constructor behavior.
/// Concrete types exercise the base class constructor and property assignments.
/// </summary>
public sealed class RateLimitPolicyBaseTests
{
    // --- None ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void NoneHasCorrectId()
    {
        var sut = new None();

        sut.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void NoneHasCorrectName()
    {
        var sut = new None();

        sut.Name.ShouldBe("None");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void NoneHasMaxIntRequests()
    {
        var sut = new None();

        sut.MaxRequests.ShouldBe(int.MaxValue);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void NoneHasZeroWindowSize()
    {
        var sut = new None();

        sut.WindowSizeInSeconds.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void NoneIsDisabled()
    {
        var sut = new None();

        sut.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void NoneHasNullDefaultRequestLimit()
    {
        var sut = new None();

        sut.DefaultRequestLimit.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void NoneHasNullDefaultTimeWindow()
    {
        var sut = new None();

        sut.DefaultTimeWindowSeconds.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void NoneDoesNotSupportBurstCapacity()
    {
        var sut = new None();

        sut.SupportsBurstCapacity.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void NoneHasNonePolicyType()
    {
        var sut = new None();

        sut.PolicyType.ShouldBe("None");
    }

    // --- FixedWindow ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FixedWindowHasCorrectId()
    {
        var sut = new FixedWindow();

        sut.Id.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FixedWindowHasCorrectName()
    {
        var sut = new FixedWindow();

        sut.Name.ShouldBe("FixedWindow");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FixedWindowHas100MaxRequests()
    {
        var sut = new FixedWindow();

        sut.MaxRequests.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FixedWindowHas60SecondWindow()
    {
        var sut = new FixedWindow();

        sut.WindowSizeInSeconds.ShouldBe(60);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FixedWindowIsEnabled()
    {
        var sut = new FixedWindow();

        sut.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FixedWindowHas100DefaultRequestLimit()
    {
        var sut = new FixedWindow();

        sut.DefaultRequestLimit.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FixedWindowHas60SecondDefaultTimeWindow()
    {
        var sut = new FixedWindow();

        sut.DefaultTimeWindowSeconds.ShouldBe(60);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FixedWindowDoesNotSupportBurstCapacity()
    {
        var sut = new FixedWindow();

        sut.SupportsBurstCapacity.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FixedWindowHasFixedWindowPolicyType()
    {
        var sut = new FixedWindow();

        sut.PolicyType.ShouldBe("FixedWindow");
    }

    // --- SlidingWindow ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void SlidingWindowHasCorrectId()
    {
        var sut = new SlidingWindow();

        sut.Id.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void SlidingWindowHasCorrectName()
    {
        var sut = new SlidingWindow();

        sut.Name.ShouldBe("SlidingWindow");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void SlidingWindowHas150MaxRequests()
    {
        var sut = new SlidingWindow();

        sut.MaxRequests.ShouldBe(150);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void SlidingWindowSupportsBurstCapacity()
    {
        var sut = new SlidingWindow();

        sut.SupportsBurstCapacity.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void SlidingWindowHas150DefaultRequestLimit()
    {
        var sut = new SlidingWindow();

        sut.DefaultRequestLimit.ShouldBe(150);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void SlidingWindowHas60SecondDefaultTimeWindow()
    {
        var sut = new SlidingWindow();

        sut.DefaultTimeWindowSeconds.ShouldBe(60);
    }

    // --- TokenBucket ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TokenBucketHasCorrectId()
    {
        var sut = new TokenBucket();

        sut.Id.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TokenBucketHasCorrectName()
    {
        var sut = new TokenBucket();

        sut.Name.ShouldBe("TokenBucket");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TokenBucketHas50MaxRequests()
    {
        var sut = new TokenBucket();

        sut.MaxRequests.ShouldBe(50);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TokenBucketHas10SecondWindow()
    {
        var sut = new TokenBucket();

        sut.WindowSizeInSeconds.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TokenBucketSupportsBurstCapacity()
    {
        var sut = new TokenBucket();

        sut.SupportsBurstCapacity.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TokenBucketHas50DefaultRequestLimit()
    {
        var sut = new TokenBucket();

        sut.DefaultRequestLimit.ShouldBe(50);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TokenBucketHas10SecondDefaultTimeWindow()
    {
        var sut = new TokenBucket();

        sut.DefaultTimeWindowSeconds.ShouldBe(10);
    }

    // --- Concurrency ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConcurrencyHasCorrectId()
    {
        var sut = new Concurrency();

        sut.Id.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConcurrencyHasCorrectName()
    {
        var sut = new Concurrency();

        sut.Name.ShouldBe("Concurrency");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConcurrencyHas10MaxRequests()
    {
        var sut = new Concurrency();

        sut.MaxRequests.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConcurrencyHasNullDefaultTimeWindow()
    {
        var sut = new Concurrency();

        sut.DefaultTimeWindowSeconds.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConcurrencyDoesNotSupportBurstCapacity()
    {
        var sut = new Concurrency();

        sut.SupportsBurstCapacity.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConcurrencyHasConcurrencyPolicyType()
    {
        var sut = new Concurrency();

        sut.PolicyType.ShouldBe("Concurrency");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConcurrencyHas10DefaultRequestLimit()
    {
        var sut = new Concurrency();

        sut.DefaultRequestLimit.ShouldBe(10);
    }
}
