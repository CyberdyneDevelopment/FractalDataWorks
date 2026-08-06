using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// Data returned by call hierarchy analysis.
/// </summary>
public sealed class CallHierarchyData
{
    /// <summary>
    /// Gets or sets the method name.
    /// </summary>
    public required string MethodName { get; init; }

    /// <summary>
    /// Gets or sets the direction (callers or callees).
    /// </summary>
    public required string Direction { get; init; }

    /// <summary>
    /// Gets or sets the call hierarchy.
    /// </summary>
    public required IReadOnlyList<CallHierarchyEntry> Hierarchy { get; init; }

    /// <summary>
    /// Gets or sets the total count of entries.
    /// </summary>
    public required int Count { get; init; }
}