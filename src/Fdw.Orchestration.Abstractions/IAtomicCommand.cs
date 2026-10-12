namespace Fdw.Orchestration.Pipelines.Abstractions;

/// <summary>
/// Base abstraction for atomic (single-operation) commands.
/// These are commands that perform a single, atomic operation.
/// Examples: Query, Insert, Transform operation, API call.
/// </summary>
public interface IAtomicCommand : ICommand
{
    // Marker interface for single operations
}