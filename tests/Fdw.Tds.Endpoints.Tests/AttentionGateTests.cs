using System.Threading;
using Fdw.Tds.Endpoints.Hosting;
using Shouldly;
using Xunit;

namespace Fdw.Tds.Endpoints.Tests;

/// <summary>
/// How an attention signal meets the request it interrupts: cancelled when one is running, queued for its own
/// acknowledgement when none is.
/// </summary>
public sealed class AttentionGateTests
{
    [Fact]
    public void AnAttentionWithNoRequestRunningIsNotTaken()
    {
        new AttentionGate().Signal().ShouldBeFalse();
    }

    [Fact]
    public void AnAttentionCancelsTheRunningRequestAndIsReportedWhenItEnds()
    {
        var gate = new AttentionGate();
        using var request = new CancellationTokenSource();
        gate.Begin(request);

        gate.Signal().ShouldBeTrue();

        request.IsCancellationRequested.ShouldBeTrue();
        gate.End().ShouldBeTrue();
    }

    [Fact]
    public void ARequestThatEndsWithoutAnAttentionReportsNone()
    {
        var gate = new AttentionGate();
        using var request = new CancellationTokenSource();
        gate.Begin(request);

        gate.End().ShouldBeFalse();
        request.IsCancellationRequested.ShouldBeFalse();
    }

    [Fact]
    public void AnAttentionAfterTheRequestEndedIsNotTaken()
    {
        var gate = new AttentionGate();
        using var request = new CancellationTokenSource();
        gate.Begin(request);
        gate.End();

        gate.Signal().ShouldBeFalse();
        request.IsCancellationRequested.ShouldBeFalse();
    }
}
