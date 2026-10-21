using System;
using Fdw.Services.RateLimiting.Abstractions;

namespace Fdw.Services.RateLimiting.Abstractions.Tests;

/// <summary>
/// Tests for the allowance a request is held to: its fingerprint is what starts a new bucket when a setting changes (FDW-1945).
/// </summary>
[Collection(nameof(RateLimitingTestCollection))]
public class RateLimitAllowanceTests
{
    private static RateLimitAllowance Allowance(
        int requests = 500,
        int seconds = 60,
        bool burst = true,
        int burstLimit = 750,
        IRateLimitAlgorithm? algorithm = null,
        int segments = 10,
        bool queue = false,
        int queueLimit = 0,
        IRateLimitPartitioning? partitioning = null)
        => new RateLimitAllowance(
            requests,
            TimeSpan.FromSeconds(seconds),
            burst,
            burstLimit,
            algorithm ?? RateLimitAlgorithms.SlidingWindow,
            segments,
            queue,
            queueLimit,
            partitioning ?? RateLimitPartitionings.PerCaller);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void TwoAllowancesWithTheSameNumbersAndNamesHaveTheSameFingerprint()
    {
        Allowance().Fingerprint.ShouldBe(Allowance().Fingerprint);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ChangingAnyOneNumberOrNameChangesTheFingerprint()
    {
        var baseline = Allowance().Fingerprint;

        Allowance(requests: 501).Fingerprint.ShouldNotBe(baseline);
        Allowance(seconds: 61).Fingerprint.ShouldNotBe(baseline);
        Allowance(burst: false).Fingerprint.ShouldNotBe(baseline);
        Allowance(burstLimit: 751).Fingerprint.ShouldNotBe(baseline);
        Allowance(algorithm: RateLimitAlgorithms.TokenBucket).Fingerprint.ShouldNotBe(baseline);
        Allowance(segments: 11).Fingerprint.ShouldNotBe(baseline);
        Allowance(queue: true).Fingerprint.ShouldNotBe(baseline);
        Allowance(queueLimit: 1).Fingerprint.ShouldNotBe(baseline);
        Allowance(partitioning: RateLimitPartitionings.Shared).Fingerprint.ShouldNotBe(baseline);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EachAlgorithmBuildsALimiterFromTheAllowance()
    {
        foreach (var algorithm in RateLimitAlgorithms.All())
        {
            using var limiter = algorithm.CreateLimiter(Allowance(algorithm: algorithm));

            limiter.ShouldNotBeNull();
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AnAllowanceWithoutAnAlgorithmOrAWayOfCountingIsRefused()
    {
        Should.Throw<ArgumentNullException>(() => new RateLimitAllowance(1, TimeSpan.FromSeconds(1), false, 1, null!, 1, false, 0, RateLimitPartitionings.Shared));
        Should.Throw<ArgumentNullException>(() => new RateLimitAllowance(1, TimeSpan.FromSeconds(1), false, 1, RateLimitAlgorithms.FixedWindow, 1, false, 0, null!));
    }
}
