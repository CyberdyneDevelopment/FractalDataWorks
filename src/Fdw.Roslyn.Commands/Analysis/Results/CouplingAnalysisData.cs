using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// Data returned by coupling analysis.
/// </summary>
public sealed class CouplingAnalysisData
{
    /// <summary>
    /// Gets or sets the type name.
    /// </summary>
    public required string TypeName { get; init; }

    /// <summary>
    /// Gets or sets the efferent coupling (Ce) - types this type depends on.
    /// </summary>
    public required int EfferentCoupling { get; init; }

    /// <summary>
    /// Gets or sets the afferent coupling (Ca) - types that depend on this type.
    /// </summary>
    public required int AfferentCoupling { get; init; }

    /// <summary>
    /// Gets or sets the instability metric.
    /// </summary>
    public required double Instability { get; init; }

    /// <summary>
    /// Gets or sets the list of efferent types.
    /// </summary>
    public required IReadOnlyList<TypeReference> EfferentTypes { get; init; }

    /// <summary>
    /// Gets or sets the list of afferent types.
    /// </summary>
    public required IReadOnlyList<TypeReference> AfferentTypes { get; init; }
}