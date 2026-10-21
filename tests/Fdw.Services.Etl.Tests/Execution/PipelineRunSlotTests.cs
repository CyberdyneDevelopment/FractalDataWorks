using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Execution;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for the pipeline run slot (FDW-1511): runs never overlap, a run that waits for the pipeline it started hands the slot
/// back so that pipeline can run, and a caller that does not hold the slot has nothing to give back.
/// </summary>
public sealed class PipelineRunSlotTests
{
    private static readonly TimeSpan Short = TimeSpan.FromMilliseconds(100);

    private static async Task<bool> CanTake(PipelineRunSlot slot)
    {
        using var timeout = new CancellationTokenSource(Short);
        try
        {
            await slot.Acquire(timeout.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task OnlyOneRunHoldsTheSlotAtATime()
    {
        using var slot = new PipelineRunSlot();
        await slot.Acquire(TestContext.Current.CancellationToken);

        (await CanTake(slot)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunThatHoldsTheSlotHandsItBackWhileItWaitsAndTakesItAgainAfterwards()
    {
        using var slot = new PipelineRunSlot();
        await slot.Acquire(TestContext.Current.CancellationToken);
        slot.MarkHeld();

        var handedBack = slot.Yield();
        var childRan = await Task.Run(async () =>
        {
            var took = await CanTake(slot);
            if (took)
            {
                slot.MarkHeld();
                slot.Release();
            }

            return took;
        }, TestContext.Current.CancellationToken);
        await handedBack.DisposeAsync();

        childRan.ShouldBeTrue();
        (await CanTake(slot)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ACallerThatDoesNotHoldTheSlotGivesNothingBack()
    {
        using var slot = new PipelineRunSlot();
        await slot.Acquire(TestContext.Current.CancellationToken);

        var handle = slot.Yield();
        var tookWhileWaiting = await CanTake(slot);
        await handle.DisposeAsync();

        tookWhileWaiting.ShouldBeFalse();
        (await CanTake(slot)).ShouldBeFalse();
    }
}
