using System.Collections.Generic;

namespace Fdw.Orchestration.Pipelines.Abstractions;

/// <summary>
/// Base abstraction for composite (multi-step) commands.
/// These are commands that orchestrate multiple operations (which may themselves be commands).
/// Examples: Pipeline, Workflow, Job sequence.
/// </summary>
public interface ICompositeCommand : ICommand
{
    /// <summary>
    /// Gets the collection of commands that compose this command.
    /// </summary>
    IReadOnlyList<ICommand> Commands { get; }
}