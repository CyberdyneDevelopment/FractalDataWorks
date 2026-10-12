using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// One path of a Branch stage, as a request states it: a name, the condition that routes a record to it, and the stages
/// a routed record passes through (possibly none).
/// </summary>
public interface ITransformPathSpec
{
    /// <summary>Gets the path's name, unique within its Branch and free of '.'.</summary>
    string Name { get; }

    /// <summary>Gets the condition a record must meet to take this path, in the pipeline's condition language.</summary>
    string Condition { get; }

    /// <summary>Gets the stages of the path, in the order they are stated; their execution order is their position among siblings.</summary>
    IReadOnlyList<ITransformOperationSpec> Stages { get; }
}
