using Fdw.Collections;

namespace Fdw.Services.Etl.Projects.Execution;

/// <summary>Where one pipeline of an orchestration node stands while the node runs its prerequisites in order (FDW-4150): one option of <see cref="PipelineRunStates"/>.</summary>
public interface IPipelineRunState : ITypeOption<int, IPipelineRunState>
{
    /// <summary>Gets a value indicating whether the pipeline has not started and may be started once its prerequisites have succeeded.</summary>
    bool WaitsToStart { get; }

    /// <summary>Gets a value indicating whether the pipeline ran and succeeded, which satisfies a pipeline that needs it.</summary>
    bool Succeeded { get; }

    /// <summary>Gets a value indicating whether a pipeline that needs this one can never start: this one failed or was not run.</summary>
    bool BlocksDependents { get; }
}
