using Fdw.Services.Etl.Execution;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for how a stopping host waits for the runs in flight (FDW-3786): the runs are given the stated time, those that finish in it are
/// counted as finished, the rest are stopped and counted as stopped, and the host waits for the stopped runs to end before it goes on.
/// </summary>
public sealed class PipelineRunDrainTests
{
    private static readonly TimeSpan Long = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Short = TimeSpan.FromMilliseconds(150);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task NoRunsInFlightMeansNothingFinishedAndNothingStopped()
    {
        var stops = 0;

        var drained = await PipelineRunDrain.Wait([Task.CompletedTask], Long, () => stops++);

        drained.ShouldBe(new PipelineRunDrained(0, 0));
        stops.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task RunsThatFinishInsideTheTimeAreCountedFinishedAndNothingIsStopped()
    {
        var stops = 0;
        var one = Task.Delay(40);
        var two = Task.Delay(80);

        var drained = await PipelineRunDrain.Wait([one, two], Long, () => stops++);

        drained.ShouldBe(new PipelineRunDrained(2, 0));
        stops.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunStillGoingWhenTheTimeEndsIsStoppedOnceAndTheHostWaitsForItToEnd()
    {
        using var stop = new CancellationTokenSource();
        var ended = false;
        var finishes = Task.Delay(30);
        var goesOn = Task.Run(
            async () =>
            {
                try
                {
                    await Task.Delay(Timeout.Infinite, stop.Token);
                }
                finally
                {
                    ended = true;
                }
            });
        var stops = 0;

        var drained = await PipelineRunDrain.Wait([finishes, goesOn], Short, () =>
        {
            stops++;
            stop.Cancel();
        });

        drained.ShouldBe(new PipelineRunDrained(1, 1));
        stops.ShouldBe(1);
        ended.ShouldBeTrue();
    }
}
