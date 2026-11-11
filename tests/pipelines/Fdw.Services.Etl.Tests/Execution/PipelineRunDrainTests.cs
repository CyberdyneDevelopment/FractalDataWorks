using Fdw.Services.Etl.Execution;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for how a stopping host waits for the runs in flight (FDW-3786): the runs are given the stated time, those that finish in it are
/// counted as finished, the rest are stopped and counted as stopped, and the host waits for the stopped runs to end before it goes on.
/// </summary>
/// <remarks>
/// No run here finishes by a timer. A run that finishes is a <see cref="TaskCompletionSource"/> the test completes, and one that never finishes
/// is a task only the stop ends, so what a run does never depends on how fast the machine is. Why completing right after Wait starts proves
/// "finished in the time": Wait runs on the calling thread up to its first await, so the runs are in flight when it counts them, and they are
/// complete well before the drain time ends. The drain time is then one only the never-ending run can pass.
/// </remarks>
public sealed class PipelineRunDrainTests
{
    private static readonly TimeSpan Long = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Short = TimeSpan.FromMilliseconds(500);

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
        var one = new TaskCompletionSource();
        var two = new TaskCompletionSource();

        var draining = PipelineRunDrain.Wait([one.Task, two.Task], Long, () => stops++);
        one.SetResult();
        two.SetResult();
        var drained = await draining;

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
        var finishes = new TaskCompletionSource();
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
            }, TestContext.Current.CancellationToken);
        var stops = 0;

        var draining = PipelineRunDrain.Wait([finishes.Task, goesOn], Short, () =>
        {
            stops++;
            stop.Cancel();
        });
        finishes.SetResult();
        var drained = await draining;

        drained.ShouldBe(new PipelineRunDrained(1, 1));
        stops.ShouldBe(1);
        ended.ShouldBeTrue();
    }
}
