using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// Data returned by the guide query.
/// </summary>
public sealed class ImplementationGuideData
{
    /// <summary>Gets the project or type the guides were resolved for.</summary>
    public required string Target { get; init; }

    /// <summary>Gets the number of guides that apply.</summary>
    public required int Count { get; init; }

    /// <summary>Gets the applicable guides.</summary>
    public required IReadOnlyList<ImplementationGuideInfo> Guides { get; init; }
}
