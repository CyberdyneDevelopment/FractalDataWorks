using Fdw.Orchestration.Abstractions.Caching;

namespace Fdw.Orchestration.Abstractions.Tests;

public class CacheEntryOptionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultValuesAreCorrect()
    {
        var sut = new CacheEntryOptions();

        sut.AbsoluteExpiration.ShouldBeNull();
        sut.AbsoluteExpirationRelativeToNow.ShouldBeNull();
        sut.SlidingExpiration.ShouldBeNull();
        sut.Priority.ShouldBe(CachePriorities.Normal);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AbsoluteExpirationCanBeSet()
    {
        var expiration = DateTimeOffset.UtcNow.AddHours(1);
        var sut = new CacheEntryOptions
        {
            AbsoluteExpiration = expiration
        };

        sut.AbsoluteExpiration.ShouldBe(expiration);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AbsoluteExpirationRelativeToNowCanBeSet()
    {
        var sut = new CacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };

        sut.AbsoluteExpirationRelativeToNow.ShouldBe(TimeSpan.FromMinutes(30));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void SlidingExpirationCanBeSet()
    {
        var sut = new CacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(5)
        };

        sut.SlidingExpiration.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void PriorityCanBeSet()
    {
        var sut = new CacheEntryOptions
        {
            Priority = CachePriorities.High
        };

        sut.Priority.ShouldBe(CachePriorities.High);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AbsoluteExpiringFactorySetsDuration()
    {
        var sut = CacheEntryOptions.AbsoluteExpiring(TimeSpan.FromMinutes(10));

        sut.AbsoluteExpirationRelativeToNow.ShouldBe(TimeSpan.FromMinutes(10));
        sut.SlidingExpiration.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void SlidingExpiringFactorySetsSlidingWindow()
    {
        var sut = CacheEntryOptions.SlidingExpiring(TimeSpan.FromMinutes(5));

        sut.SlidingExpiration.ShouldBe(TimeSpan.FromMinutes(5));
        sut.AbsoluteExpirationRelativeToNow.ShouldBeNull();
    }
}

public class CachePriorityTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void LowHasValueZero()
    {
        CachePriorities.Low.Id.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void NormalHasValueOne()
    {
        CachePriorities.Normal.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void HighHasValueTwo()
    {
        CachePriorities.High.Id.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void NeverRemoveHasValueThree()
    {
        CachePriorities.NeverRemove.Id.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void PrioritiesAreOrdered()
    {
        CachePriorities.Low.Id.ShouldBeLessThan(CachePriorities.Normal.Id);
        CachePriorities.Normal.Id.ShouldBeLessThan(CachePriorities.High.Id);
        CachePriorities.High.Id.ShouldBeLessThan(CachePriorities.NeverRemove.Id);
    }
}
