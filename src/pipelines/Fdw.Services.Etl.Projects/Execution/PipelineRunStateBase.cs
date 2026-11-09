using Fdw.Collections;

namespace Fdw.Services.Etl.Projects.Execution;

/// <summary>Base of the <see cref="PipelineRunStates"/> options.</summary>
public abstract class PipelineRunStateBase : TypeOptionBase<PipelineRunStateBase>, IPipelineRunState
{
    /// <summary>Initializes a new instance of the <see cref="PipelineRunStateBase"/> class.</summary>
    /// <param name="id">The option's id, unique in the collection.</param>
    /// <param name="name">The option's name.</param>
    /// <param name="waitsToStart">Whether the pipeline has not started and may be started.</param>
    /// <param name="succeeded">Whether the pipeline ran and succeeded.</param>
    /// <param name="blocksDependents">Whether a pipeline that needs this one can never start.</param>
    protected PipelineRunStateBase(int id, string name, bool waitsToStart, bool succeeded, bool blocksDependents)
        : base(id, name)
    {
        WaitsToStart = waitsToStart;
        Succeeded = succeeded;
        BlocksDependents = blocksDependents;
    }

    /// <inheritdoc />
    public bool WaitsToStart { get; }

    /// <inheritdoc />
    public bool Succeeded { get; }

    /// <inheritdoc />
    public bool BlocksDependents { get; }
}
