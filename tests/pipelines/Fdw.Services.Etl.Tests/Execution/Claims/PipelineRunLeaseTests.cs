using System;
using System.Linq;
using Fdw.Services.Etl.Execution.Claims;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution.Claims;

/// <summary>
/// FDW-6629: the holder of a claim fences itself. It measures its lease with a monotonic clock from the moment the claim was granted or last renewed, and before every step of the run
/// it asks whether the lease still holds: past it, the claim may already be another run's, so it stops.
/// </summary>
public sealed class PipelineRunLeaseTests
{
    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _ticks;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _ticks;

        public void Advance(TimeSpan by) => _ticks += by.Ticks;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ALeaseHoldsUntilItsDurationHasPassed()
    {
        var clock = new ManualTimeProvider();
        var lease = PipelineRunLease.Start(clock, TimeSpan.FromSeconds(30));

        clock.Advance(TimeSpan.FromSeconds(29));

        lease.Check(NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void APastLeaseStopsTheRunBeforeItsNextStepWithItsCode()
    {
        var clock = new ManualTimeProvider();
        var lease = PipelineRunLease.Start(clock, TimeSpan.FromSeconds(30));

        clock.Advance(TimeSpan.FromSeconds(30));
        var check = lease.Check(NullLogger.Instance);

        check.IsFailure.ShouldBeTrue();
        check.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60868");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARenewalRestartsTheMeasure()
    {
        var clock = new ManualTimeProvider();
        var lease = PipelineRunLease.Start(clock, TimeSpan.FromSeconds(30));

        clock.Advance(TimeSpan.FromSeconds(20));
        lease.Renewed();
        clock.Advance(TimeSpan.FromSeconds(20));

        lease.Check(NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ALeaseOfNoDurationIsNotBuilt()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PipelineRunLease.Start(new ManualTimeProvider(), TimeSpan.Zero));
    }
}
