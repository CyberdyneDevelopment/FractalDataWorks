using System;
using System.Threading;

namespace Fdw.Services.Pipelines.Abstractions.Runs;

/// <summary>
/// The limits on one run's time and steps (FDW-6141): a deadline and the steps left. One object, carried by the run, so one place counts and one place stops.
/// </summary>
/// <remarks>
/// The brief's word for them is limits ("stated limits on time and on steps"); budget stays free for tokens. A step is taken before it runs (<see cref="TryTakeStep"/>). A child run
/// an at-once run starts holds the parent's object (<see cref="ForChild"/>): its steps are taken from its own count and from every parent above it, and its deadline is the earlier
/// of the parent's and its own, so what a child's pipeline states can only shorten the turn's limits, never extend them.
/// </remarks>
public sealed class PipelineRunTimeAndStepLimits
{
    private readonly PipelineRunTimeAndStepLimits? _parent;
    private int _stepsLeft;

    private PipelineRunTimeAndStepLimits(DateTimeOffset deadline, int steps, PipelineRunTimeAndStepLimits? parent)
    {
        Deadline = deadline;
        _stepsLeft = steps;
        _parent = parent;
    }

    /// <summary>Gets the moment the run must have ended by.</summary>
    public DateTimeOffset Deadline { get; }

    /// <summary>Gets how many steps this run may still take.</summary>
    public int StepsLeft => Volatile.Read(ref _stepsLeft);

    /// <summary>Makes the limits of a run that no other run started.</summary>
    /// <param name="startedAt">When the run started.</param>
    /// <param name="maxRunSeconds">The most seconds the run may take.</param>
    /// <param name="maxSteps">The most steps the run may take.</param>
    /// <returns>The limits.</returns>
    public static PipelineRunTimeAndStepLimits Start(DateTimeOffset startedAt, int maxRunSeconds, int maxSteps)
        => new(startedAt.AddSeconds(maxRunSeconds), maxSteps, parent: null);

    /// <summary>Makes the limits of a child run: the parent's, narrowed by what the child's own pipeline states.</summary>
    /// <param name="startedAt">When the child started.</param>
    /// <param name="maxRunSeconds">The most seconds the child's pipeline states, or null when it states none.</param>
    /// <param name="maxSteps">The most steps the child's pipeline states, or null when it states none.</param>
    /// <returns>The child's limits, never longer or larger than the parent's.</returns>
    public PipelineRunTimeAndStepLimits ForChild(DateTimeOffset startedAt, int? maxRunSeconds, int? maxSteps)
    {
        var own = maxRunSeconds is { } seconds ? startedAt.AddSeconds(seconds) : Deadline;
        var remaining = StepsLeft;
        return new PipelineRunTimeAndStepLimits(own < Deadline ? own : Deadline, maxSteps is { } steps && steps < remaining ? steps : remaining, this);
    }

    /// <summary>Takes one step.</summary>
    /// <returns><see langword="true"/> when this run and every run above it had a step left; <see langword="false"/> when none is left, and nothing is taken.</returns>
    public bool TryTakeStep()
    {
        if (!TakeOwn())
            return false;

        if (_parent is not null && !_parent.TryTakeStep())
        {
            Interlocked.Increment(ref _stepsLeft);
            return false;
        }

        return true;
    }

    private bool TakeOwn()
    {
        while (true)
        {
            var left = Volatile.Read(ref _stepsLeft);
            if (left <= 0)
                return false;

            if (Interlocked.CompareExchange(ref _stepsLeft, left - 1, left) == left)
                return true;
        }
    }
}
