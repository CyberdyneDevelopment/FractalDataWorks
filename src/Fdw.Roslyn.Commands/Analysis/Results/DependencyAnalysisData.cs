using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// Data returned by dependency analysis.
/// </summary>
public sealed class DependencyAnalysisData
{
    /// <summary>
    /// Gets or sets the type name.
    /// </summary>
    public required string TypeName { get; init; }

    /// <summary>
    /// Gets or sets the list of dependencies.
    /// </summary>
    public required IReadOnlyList<TypeDependency> Dependencies { get; init; }

    /// <summary>
    /// Gets or sets the total count of dependencies.
    /// </summary>
    public required int Count { get; init; }
}