using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// One place a project or type departs from an applicable guide.
/// </summary>
public sealed class AuditFindingInfo
{
    /// <summary>Gets the id of the guide whose rule it is.</summary>
    public required string Guide { get; init; }

    /// <summary>Gets what the finding is about.</summary>
    public required AuditFindingKind Kind { get; init; }

    /// <summary>Gets the assembly or member concerned.</summary>
    public required string Subject { get; init; }

    /// <summary>Gets what is wrong.</summary>
    public required string Message { get; init; }

    /// <summary>Gets the source line the rule rests on; null for a member finding.</summary>
    public required string? Cite { get; init; }
}
