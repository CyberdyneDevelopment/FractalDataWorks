using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// Data returned by the audit.
/// </summary>
public sealed class AuditImplementationData
{
    /// <summary>Gets the project audited.</summary>
    public required string Project { get; init; }

    /// <summary>Gets the number of guides that applied.</summary>
    public required int GuideCount { get; init; }

    /// <summary>Gets every mechanically checked departure.</summary>
    public required IReadOnlyList<AuditFindingInfo> Findings { get; init; }

    /// <summary>Gets the must and must-not rules this audit cannot evaluate, listed for review.</summary>
    public required IReadOnlyList<GuideRuleInfo> ManualChecks { get; init; }
}
